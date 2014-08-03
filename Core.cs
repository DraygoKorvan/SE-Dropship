using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;
using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Server;
using SEModAPIInternal.Support;

using SEModAPI.API;

using VRageMath;
using VRage.Common.Utils;



namespace SEDropship
{
	[Serializable()]
	public class SEDropshipSettings
	{
		private int m_slowDownDistance = 250;
		private bool m_anyAsteroid = true;
		private int m_resolution = 1000;
		private float m_slowSpeed = 5.0F;
		private float m_startSpeed = 104.4F;
		private int m_countdown = 10;
		public int slowDownDistance
		{
			get { return m_slowDownDistance; }
			set { if (value > 250) m_slowDownDistance = value; else m_slowDownDistance = 250; }//safe minimum
		}

		public bool anyAsteroid
		{
			get { return m_anyAsteroid; }
			set { m_anyAsteroid = value; }
		}
		public int resolution
		{
			get { return m_resolution; }
			set { if (value > 0 ) m_resolution = value; }
		}
		public float slowSpeed
		{
			get { return m_slowSpeed; }
			set { if (value >= 0F) m_slowSpeed = value; }
		}
		public float startSpeed
		{
			get { return m_startSpeed; }
			set { if(value >= 0F) m_startSpeed = value; }
		}
		public int countdown
		{
			get { return m_countdown; }
			set { if(value >= 0) m_countdown = value; }
		}
	}
	[Serializable()]
	public class SeDropshipAsteroids
	{
		private VoxelMap m_asteroid;
		private int m_sizex = 50;
		private int m_sizey = 50;
		private int m_sizez = 50;

		public SeDropshipAsteroids(VoxelMap asteroid)
		{
			m_asteroid = asteroid;
			calculateSize();
		}
		public SeDropshipAsteroids(VoxelMap asteroid, int x, int y, int z)
		{
			m_sizex = x;
			m_sizey = y;
			m_sizez = z;
			m_asteroid = asteroid;
		}

		private void calculateSize()
		{
			if (m_asteroid.Filename.Contains("central"))
			{
				m_sizex = 250;
				m_sizey = 250;
				m_sizez = 250;
			}
			else
			{
				m_sizex = 50;
				m_sizey = 50;
				m_sizez = 50;
			}
		}

		[Browsable(true)]
		[ReadOnly(true)]
		public string Location
		{
			get { return SandboxGameAssemblyWrapper.Instance.GetServerConfig().LoadWorld + "\\"; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public VoxelMap asteroid
		{
			get { return m_asteroid; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public int x
		{
			get { return m_sizex; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public int y
		{
			get { return m_sizey; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public int z
		{
			get { return m_sizez; }

		}
	}
	public class SEDropship : PluginBase, IChatEventHandler
	{
		
		#region "Attributes"

		private Thread m_main;
		private SEDropshipSettings settings;
		private Random m_gen;
		private List<SeDropshipAsteroids> m_asteroids = new List<SeDropshipAsteroids>();
		bool m_running = true;

		#endregion

		#region "Constructors and Initializers"

		public void Core()
		{
			Console.WriteLine("SE Dropship Plugin '" + Id.ToString() + "' constructed!");	
		}

		public override void Init()
		{
			//find all dropships and ignore.

			settings = new SEDropshipSettings();
			loadXML(false);
			m_gen = new Random();
			m_running = true;
			//build asteroid list
			m_main = new Thread(mainloop);
			m_main.Start();
			m_main.Priority = ThreadPriority.BelowNormal;//lower priority to make room for other tasks if needed.
			
			Console.WriteLine("SE Dropship Plugin '" + Id.ToString() + "' initialized!");	
		}

		#endregion

		#region "Properties"


		[Browsable(true)]
		[ReadOnly(true)]
		public string DefaultLocation
		{
			get { return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\"; }

		}		
		[Browsable(true)]
		[ReadOnly(true)]
		public string Location
		{
			get { return SandboxGameAssemblyWrapper.Instance.GetServerConfig().LoadWorld  + "\\"; }
		
		}

		[Category("SE Dropship")]
		[Description("Target Any Asteroid")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool anyAsteroid
		{
			get { return settings.anyAsteroid; }
			set { settings.anyAsteroid = value; }
		}

		[Category("SE Dropship")]
		[Description("Resolution settings, 1 second = 1000")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int resolution
		{
			get { return settings.resolution; }
			set { settings.resolution = value; }
		}

		[Category("SE Dropship")]
		[Description("How fast the ship goes when it is near its target")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float slowSpeed
		{
			get { return settings.slowSpeed; }
			set { settings.slowSpeed = value; }
		}

		[Category("SE Dropship")]
		[Description("Initial dropship speed")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float startSpeed
		{
			get { return settings.startSpeed; }
			set { settings.startSpeed = value; }
		}

		[Category("SE Dropship")]
		[Description("Distance from target before dropship slows down")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int slowDownDistance
		{
			get { return settings.slowDownDistance; }
			set { settings.slowDownDistance = value; }
		}
		[Category("SE Dropship")]
		[Description("Countdown, sets amount of seconds for insertion sequence to start")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int countdown
		{
			get { return settings.countdown; }
			set { settings.countdown = value; }
		}		

		#endregion

		#region "Methods"

		private void mainloop()
		{
			Thread.Sleep(resolution);
			m_asteroids.Clear();
			List<VoxelMap> asteroids = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
			foreach (VoxelMap voxelmap in asteroids)
			{
				if (!voxelmap.Filename.Contains("moon"))
				{
					//ignore moons				
					m_asteroids.Add(new SeDropshipAsteroids(voxelmap));
				}
			}
			List<CubeGridEntity> ignore = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>();
			while(m_running)
			{
				try
				{
					Thread.Sleep(resolution); //update the resolution to 1 second
					List<CubeGridEntity> huntlist = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>();
					foreach (CubeGridEntity grid in huntlist)
					{
						Thread.Yield();//yield processing to other things
						if (grid.IsLoading) continue;
						if (ignore.Exists(x => x.EntityId == grid.EntityId))
						{
							continue;
						}
						ignore.Add(grid);
						if (ServerNetworkManager.Instance.GetConnectedPlayers().Count == 0) continue;
						long  tempowner = 0;
						bool found = false;
						CockpitEntity cockpit = null;
						foreach (CubeBlockEntity cubein in grid.CubeBlocks)
						{
							if (cubein.Owner != 0) tempowner = cubein.Owner;
							
							if (cubein is CockpitEntity)
							{
								cockpit = (CockpitEntity)cubein;
								if (cockpit.CustomName == "Dropship")
								{
									found = true;
									
								}
							}
						}
						if(found && cockpit != null)
						{
							Thread dropstep = new Thread(() => doDrop(grid, cockpit, tempowner));
							dropstep.Priority = ThreadPriority.BelowNormal;
							dropstep.Start();
						}
					}
				}
				catch (Exception ex) 
				{ 
					LogManager.APILog.WriteLineAndConsole( ex.ToString()); 
				}
			}

		}
		private Vector3 FindInterceptVector(Vector3 spawnOrigin, float meteoroidSpeed, Vector3 targetOrigin, Vector3 targetVel)
		{

			Vector3 dirToTarget = Vector3.Normalize(targetOrigin - spawnOrigin);
			Vector3 targetVelOrth = Vector3.Dot(targetVel, dirToTarget) * dirToTarget;
			Vector3 targetVelTang = targetVel - targetVelOrth;
			Vector3 shotVelTang = targetVelTang;
			float shotVelSpeed = shotVelTang.Length();
			if (shotVelSpeed > meteoroidSpeed)
			{
				return Vector3.Multiply(targetVel, meteoroidSpeed);
			}
			else
			{
				float shotSpeedOrth = (float)Math.Sqrt(meteoroidSpeed * meteoroidSpeed - shotVelSpeed * shotVelSpeed);
				Vector3 shotVelOrth = dirToTarget * shotSpeedOrth;
				return shotVelOrth + shotVelTang;
			}
		}
		public void doDrop(CubeGridEntity grid, CockpitEntity seat, long Owner)
		{
			//find target
			List<VoxelMap> asteroids = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
			//int asteroidcount = asteroids.Count();
			if (m_asteroids.Count == 0)
			{
				LogManager.APILog.WriteLineAndConsole("No asteroids, aborting drop");
				return;
			}
			SeDropshipAsteroids asteroid = m_asteroids.First();
			//pick a random asteroid if desired: going to change this up later
			if(anyAsteroid)
			{
				asteroid = m_asteroids.ElementAt(m_gen.Next(m_asteroids.Count));
			}

			Vector3Wrapper target = asteroid.asteroid.Position;
			List<ulong> steamlist = ServerNetworkManager.Instance.GetConnectedPlayers();
			ulong steamid = 0;
			foreach( ulong steam_id in steamlist)
			{
				List<long> playerids =  PlayerMap.Instance.GetPlayerIdsFromSteamId(steam_id);
				//PlayerMap.Instance.get
				foreach (long playerid in playerids)
				{
					if(playerid == Owner)
						steamid = steam_id;
				}
			}
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Dropship booting up, please stay in your seat for insertion.");
			Thread.Sleep(1000);
			ChatManager.Instance.SendPrivateChatMessage(steamid, "If you exited your ship please return to the passenger seat before the countdown begins.");
			Thread.Sleep(5000);
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Dropship Sequence Initiated, please remain seated. Exiting your seat will abort automatic insertion.");
			Thread.Sleep(1000);
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Beginning Insertion Sequence.");
			Thread.Sleep(1000);	
			for ( int count = countdown; count > 0; count--)
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, count.ToString() + ".");
				if (seat.Pilot == null)
					break;
				Thread.Sleep(1000);
			}
			if(seat.Pilot != null)
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, "Insertion Sequence initiated.");
			}
			else
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, "Insertion Sequence aborted.");
				return;
			}
			Vector3 adjustVector = new Vector3Wrapper(asteroid.x, asteroid.y, asteroid.z);
			target = Vector3.Add(asteroid.asteroid.Position, adjustVector);//temp, till we can query asteroid size.
			float adjust = Vector3.Distance(new Vector3Wrapper(0,0,0), adjustVector);
			Vector3Wrapper position = grid.Position;
			Vector3Wrapper Vector3Intercept = (Vector3Wrapper)FindInterceptVector(position, startSpeed, target, new Vector3Wrapper(0, 0, 0));
			grid.Forward = Vector3.Normalize(Vector3Intercept);
			grid.LinearVelocity = Vector3Intercept;
			float timeToSlow = Vector3.Distance(position, Vector3.Subtract(target, Vector3.Multiply(Vector3.Normalize(Vector3Intercept), slowDownDistance+adjust))) / Vector3.Distance(new Vector3Wrapper(0, 0, 0), Vector3Intercept);
			float timeToCollision = Vector3.Distance(Vector3.Subtract(target, Vector3.Multiply(Vector3.Normalize(Vector3Intercept), slowDownDistance+adjust)), Vector3.Subtract(target, Vector3.Multiply(Vector3.Normalize(Vector3Intercept), 10))) / slowSpeed;
			Thread.Sleep((int)timeToSlow * 1000);
			grid.LinearVelocity = Vector3.Multiply(Vector3.Normalize(Vector3Intercept), slowSpeed);
			Thread.Sleep((int)timeToCollision * 1000);
			grid.LinearVelocity = new Vector3Wrapper(0, 0, 0);
			
		}
		public void saveXML()
		{

			XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
			TextWriter writer = new StreamWriter(Location + "SE-Dropship-Settings.xml");
			x.Serialize(writer, settings);
			writer.Close();

		}
		public void loadXML(bool defaults = false)
		{
			try
			{
				if (File.Exists(Location + "SE-Dropship-Settings.xml") && !defaults)
				{

					XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
					TextReader reader = new StreamReader(Location + "SE-Dropship-Settings.xml");
					SEDropshipSettings obj = (SEDropshipSettings)x.Deserialize(reader);
	
					reader.Close();
					return;
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}
			try
			{
				if (File.Exists(DefaultLocation + "SE-Dropship-Settings.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
					TextReader reader = new StreamReader(DefaultLocation + "SE-Dropship-Settings.xml");
					SEDropshipSettings obj = (SEDropshipSettings)x.Deserialize(reader);

					reader.Close();
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}

		}


		#region "EventHandlers"

		public override void Update()
		{

		}

		public override void Shutdown()
		{
			m_running = false;
			saveXML();
			return;
		}

		public void OnChatReceived(SEModAPIExtensions.API.ChatManager.ChatEvent obj)
		{

			if (obj.sourceUserId == 0)
				return;
			bool isadmin = SandboxGameAssemblyWrapper.Instance.IsUserAdmin(obj.sourceUserId);

			if( obj.message[0] == '/' )
			{

				string[] words = obj.message.Split(' ');
				//string rem;
				//proccess
				

				if (isadmin && words[0] == "/se-dropship-save")
				{

					saveXML();
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Se-dropship Configuration Saved.");
					return;
				}
				if (isadmin && words[0] == "/se-dropship-load")
				{
					loadXML(false);
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Se-dropship Configuration Loaded.");
					return;
				}
				if (isadmin && words[0] == "/se-dropship-loaddefault")
				{
					loadXML(true);
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Se-dropship Configuration Defaults Loaded.");
					return;
				}
			}
			return; 
		}

		public void OnChatSent(SEModAPIExtensions.API.ChatManager.ChatEvent obj)
		{
			return; 
		}


		#endregion



		#endregion
	}
}
