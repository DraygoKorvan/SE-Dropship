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
	}
	public class SEDropship : PluginBase, IChatEventHandler
	{
		
		#region "Attributes"

		private Thread main;
		private SEDropshipSettings settings;
		private Random m_gen;
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
			main = new Thread(mainloop);
			main.Start();
			main.Priority = ThreadPriority.BelowNormal;//lower priority to make room for other tasks if needed.
			
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

		#endregion

		#region "Methods"

		private void mainloop()
		{
			Thread.Sleep(1000);
			List<CubeGridEntity> ignore = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>();
			while(m_running)
			{
				try
				{
					Thread.Sleep(1000); //update the resolution to 1 second
					List<CubeGridEntity> huntlist = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>();
					foreach (CubeGridEntity grid in huntlist)
					{
						if (grid.IsLoading) continue;
						if (ignore.Exists(x => x.EntityId == grid.EntityId))
						{
							continue;
						}
						ignore.Add(grid);
						if (ServerNetworkManager.Instance.GetConnectedPlayers().Count == 0) continue;
						foreach (CubeBlockEntity cubein in grid.CubeBlocks)
						{
							if (cubein is CockpitEntity)
							{
								CockpitEntity cockpit = (CockpitEntity)cubein;
								if (cockpit.CustomName == "Dropship")
								{
									Thread dropstep = new Thread(() => doDrop(grid));
									dropstep.Priority = ThreadPriority.BelowNormal;
									dropstep.Start();
								}
							}
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
			Vector3 targetVelOrth =
			Vector3.Dot(targetVel, dirToTarget) * dirToTarget;
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
		public void doDrop(CubeGridEntity grid)
		{
			//find target
			List<VoxelMap> asteroids = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
			//int asteroidcount = asteroids.Count();
			VoxelMap asteroid = asteroids.First();
			//pick a random asteroid if desired:
			if(anyAsteroid)
			{
				VoxelMap roid = asteroids.ElementAt(m_gen.Next(asteroids.Count));
				if (!roid.Filename.Contains("moon") ) //if it is a moon ignore.
					asteroid = roid;
			}

			Vector3Wrapper target = asteroid.Position;
			

			target = Vector3.Add(asteroid.Position, new Vector3Wrapper(50, 50, 50));//temp, till we can query asteroid size.
			Vector3Wrapper position = grid.Position;
			Vector3Wrapper Vector3Intercept = (Vector3Wrapper)FindInterceptVector(position, 104.4F, target, new Vector3Wrapper(0, 0, 0));
			grid.Forward = Vector3.Normalize(Vector3Intercept);
			grid.LinearVelocity = Vector3Intercept;
			float timeToCollision = Vector3.Distance(position, Vector3.Subtract(target, Vector3.Multiply(Vector3.Normalize(Vector3Intercept), settings.slowDownDistance))) / Vector3.Distance(new Vector3Wrapper(0, 0, 0), Vector3Intercept);
			Thread.Sleep((int)timeToCollision * 1000);
			grid.LinearVelocity = Vector3.Multiply(Vector3.Normalize(Vector3Intercept), 5.0F);
		}
		public void saveXML()
		{

			XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
			TextWriter writer = new StreamWriter(Location + "SEMotd-Config.xml");
			x.Serialize(writer, settings);
			writer.Close();

		}
		public void loadXML(bool defaults = false)
		{
			try
			{
				if (File.Exists(Location + "SEMotd-Config.xml") && !defaults)
				{

					XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
					TextReader reader = new StreamReader(Location + "SEMotd-Config.xml");
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
				if (File.Exists(DefaultLocation + "SEMotd-Config.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
					TextReader reader = new StreamReader(DefaultLocation + "SEMotd-Config.xml");
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

		public void OnPlayerJoined(ulong nothing, CharacterEntity character)
		{

		}
		public void OnPlayerLeft(ulong nothing, CharacterEntity character)
		{
			return;
		}
		#endregion



		#endregion
	}
}
