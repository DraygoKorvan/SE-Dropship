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
using Sandbox.Definitions;

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
using VRage.Collections;


namespace SEDropship
{
	public class SEDropship : PluginBase, ICubeGridHandler
	{
		
		#region "Attributes"

		private Thread m_main;
		private SEDropshipSettings settings;
		private Random m_gen;
		private AsteroidCollection m_asteroids = new AsteroidCollection();
		bool m_running = true;
		bool m_loading = true;
		List<long> m_ignore = new List<long>();
		private bool m_debugging = false;
		private int m_debuglevel = 1;

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
			m_loading = true;
			//build asteroid list
			m_ignore.Clear();
			m_main = new Thread(mainloop);
			m_main.Start();
			m_main.Priority = ThreadPriority.BelowNormal;//lower priority to make room for other tasks if needed.

			//Register Chat Commands
			ChatManager.ChatCommand command = new ChatManager.ChatCommand();
			command.callback = saveXML;
			command.command = "se-dropship-save";
			command.requiresAdmin = true;
			ChatManager.Instance.RegisterChatCommand(command);

			command = new ChatManager.ChatCommand();
			command.callback = loadXML;
			command.command = "se-dropship-load";
			command.requiresAdmin = true;
			ChatManager.Instance.RegisterChatCommand(command);

			command = new ChatManager.ChatCommand();
			command.callback = loadDefaults;
			command.command = "se-dropship-loaddefaults";
			command.requiresAdmin = true;
			ChatManager.Instance.RegisterChatCommand(command);
			//End Register Chat commands			

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

		[Category("Asteroid")]
		[Description("Target Any Asteroid")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool anyAsteroid
		{
			get { return settings.anyAsteroid; }
			set { settings.anyAsteroid = value; }
		}
		[Category("Asteroid")]
		[Description("Plugin will ignore asteroid if this keyword exists in the asteroid filename.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string ignoreKeyword
		{
			get { return settings.ignoreKeyword; }
			set { settings.ignoreKeyword = value; }
		}
		[Category("Asteroid")]
		[Description("Requires target asteroid to have Stone, Iron, Nickel, Cobalt, Silver, Gold, Uranium, Platinum, and Silicon.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool requireAllVitalMats
		{
			get { return settings.requireVital; }
			set { settings.requireVital = value; }
		}
		[Category("Asteroid")]
		[Description("Requires target asteroid to have Magnesium.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool requireMagnesium
		{
			get { return settings.requireMagnesium; }
			set { settings.requireMagnesium = value; }
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
		[Category("SE Dropship")]
		[Description("Delete dropship if aborted")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool deleteIfAbort
		{
			get { return settings.deleteIfAbort; }
			set { settings.deleteIfAbort = value; }
		}
		[Category("SE Dropship")]
		[Description("Distance from target to teleport the pod to.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int teleportDistance
		{
			get { return settings.teleportDistance; }
			set { settings.teleportDistance = value; }
		}
		[Category("Debug")]
		[Description("List of allowed asteroids")]
		[Browsable(true)]
		[ReadOnly(true)]
		[TypeConverter(typeof(AsteroidsConverter))]
		public AsteroidCollection AsteroidList
		{
			get { return m_asteroids; }
		}
		[Category("Debug")]
		[Description("Loading")]
		[Browsable(true)]
		[ReadOnly(true)]
		public bool Loading
		{
			get { return m_loading; }
		}
		[Category("Debug")]
		[Description("is debugging")]
		[Browsable(true)]
		[ReadOnly(true)]
		public bool isdebugging
		{
			get { return m_debugging || SandboxGameAssemblyWrapper.IsDebugging; }
		}
		[Category("Debug")]
		[Description("Debug Output")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool debugging
		{
			get { return m_debugging; }
			set { m_debugging = value; }
		}
		[Category("Debug")]
		[Description("Debug Level")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int debugLevel
		{
			get { return m_debuglevel; }
			set { m_debuglevel = value; }
		}

		#endregion

		#region "Methods"

		#region "Core"
		private void mainloop()
		{
			//Thread.Sleep(resolution);
			m_asteroids.Clear();
			List<VoxelMap> asteroids = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();
			do
			{
				Thread.Sleep(1000);
				asteroids = SectorObjectManager.Instance.GetTypedInternalData<VoxelMap>();

			}
			while (asteroids.Count == 0);

			foreach (VoxelMap voxelmap in asteroids)
			{
				if (ignoreKeyword == null)
					ignoreKeyword = "";
				if (!voxelmap.Filename.Contains("moon") && !voxelmap.Filename.Contains("ignore") &&  (ignoreKeyword == "" || !voxelmap.Filename.Contains(ignoreKeyword)) )
				{
					//ignore moons	
					bool has_stone = false;
					bool has_iron = false;
					bool has_silver = false;
					bool has_gold = false;
					bool has_nickel =false;
					bool has_cobalt = false;
					bool has_platinum = false;
					bool has_magnesium = false;
					bool has_uranium = false;
					bool has_silicon = false;

					foreach (var material in voxelmap.Materials)
					{
						switch (material.Key.Id.SubtypeName)
						{
							case "Stone_01":
							case "Stone_02":
							case "Stone_03":
							case "Stone_04":
							case "Stone_05":
								has_stone = true;
								break;
							case "Iron_01":
							case "Iron_02":
								has_iron = true;
								break;
							case "Nickel_01":
								has_nickel = true;
								break;
							case "Silver_01":
								has_silver = true;
								break;
							case "Gold_01":
								has_gold = true;
								break;
							case "Platinum_01":
								has_platinum = true;
								break;
							case "Uraninite_01":
								has_uranium = true;
								break;
							case "Silicon_01":
								has_silicon = true;
								break;
							case "Magnesium_01":
								has_magnesium = true;
								break;
							case "Cobalt_01":
								has_cobalt = true;
								break;
							default:
								Console.WriteLine("Dropship Plugin Warning: Could not identify Unknown Material: " + material.Key.Id.SubtypeName.ToString());
								break;
						}
					}
					if((requireMagnesium && has_magnesium) || !requireMagnesium)
					{
						if(( requireAllVitalMats && has_iron && has_silicon && has_silver && has_gold && has_nickel && has_cobalt && has_platinum && has_uranium && has_stone) || !requireAllVitalMats)
							m_asteroids.Add(new SeDropshipAsteroids(voxelmap));
						else
							Console.WriteLine("Dropship Plugin Warning: Could not register astreroid, lacking required materials. " + voxelmap.Filename.ToString());
					}
					else
					{
						Console.WriteLine("Dropship Plugin Warning: Could not register astreroid, lacking required materials. " + voxelmap.Filename.ToString());
					}

						
				}
			}
			List<CubeGridEntity> ignore = SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>();
			foreach (CubeGridEntity grid in ignore)
			{
				m_ignore.Add(grid.EntityId);
			}
			m_loading = false;

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
		private void OnCubeGridDetected(CubeGridEntity grid)
		{
			if(isdebugging)
				LogManager.APILog.WriteLineAndConsole("OnCubeGridDetected: " + grid.DisplayName.ToString());
			if (m_loading) return;
			long _entityId = grid.EntityId;
			try
			{
				while (grid.IsLoading)
				{
					Thread.Sleep(resolution);
					grid = (CubeGridEntity)GameEntityManager.GetEntity(_entityId);
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Failed to start up dropship." + ex.ToString());
				return;
			}
			if (isdebugging)
				LogManager.APILog.WriteLineAndConsole("OnCubeGridDetected: - Checking Ignore List -" + grid.DisplayName.ToString());
			if (m_ignore.Exists(x => x == grid.EntityId))
			{
				return;
			}
			m_ignore.Add(grid.EntityId);
			if (isdebugging)
				LogManager.APILog.WriteLineAndConsole("OnCubeGridDetected: - Checking Connected Players - " + grid.DisplayName.ToString());
			if (ServerNetworkManager.Instance.GetConnectedPlayers().Count == 0) return;

			long tempowner = 0;
			bool found = false;
			CockpitEntity cockpit = null;
			if (isdebugging)
				LogManager.APILog.WriteLineAndConsole("OnCubeGridDetected: - Searching for cockpit with Dropship name - " + grid.DisplayName.ToString());
			foreach (CubeBlockEntity cubein in grid.CubeBlocks)
			{
				if (cubein.Owner != 0) tempowner = cubein.Owner;

				if (cubein is CockpitEntity)
				{

					CockpitEntity t_cockpit = (CockpitEntity)cubein;
					if (t_cockpit.CustomName == "Dropship")
					{
						if (isdebugging)
							LogManager.APILog.WriteLineAndConsole("OnCubeGridDetected: - found cockpit - " + grid.DisplayName.ToString());
						cockpit = t_cockpit;
						found = true;
					}
				}
			}
			if (found && cockpit != null)
			{
				if (isdebugging)
					LogManager.APILog.WriteLineAndConsole("OnCubeGridDetected: - launching dropship thread - " + grid.DisplayName.ToString());
				Thread dropstep = new Thread(() => doDrop(grid, cockpit, tempowner));
				dropstep.Priority = ThreadPriority.BelowNormal;
				dropstep.Start();
			}		
		}
		public void doDrop(CubeGridEntity grid, CockpitEntity seat, long Owner)
		{
			Console.WriteLine("DoDrop called.");
			//find target
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
				foreach (long playerid in playerids)
				{
					if(playerid == Owner)
						steamid = steam_id;
				}
			}
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Dropship booting up, please stay in your seat for insertion.");
			Thread.Sleep(1000);
			ChatManager.Instance.SendPrivateChatMessage(steamid, "If you exited your ship please return to the passenger seat before the countdown finishes.");
			Thread.Sleep(2000);
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Dropship Sequence Initiated, please remain seated. Exiting your seat will abort automatic insertion.");
			Thread.Sleep(1000);
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Beginning Insertion Sequence.");
			Thread.Sleep(1000);	
			for ( int count = countdown; count > 0; count--)
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, count.ToString() + ".");
				Thread.Sleep(1000);
			}
			if(seat.PilotEntity != null)
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, "Insertion Sequence initiated.");
			}
			else
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, "Insertion Sequence aborted." + ( deleteIfAbort ? " Dropship self destructing." : ""));
				if (deleteIfAbort) 
					grid.Dispose();

				return;
			}
			Vector3I startadjustVector = new Vector3I(asteroid.x/2, asteroid.y/2, asteroid.z/2);
			MyVoxelMaterialDefinition mat = asteroid.asteroid.GetMaterial(startadjustVector);
			Vector3 dir = Vector3.Normalize(new Vector3((float)(m_gen.NextDouble() * 2 - 1), (float)(m_gen.NextDouble() * 2 - 1), (float)(m_gen.NextDouble() * 2 - 1)));
			int trycount = 0;
			Vector3 step = dir * trycount;
			Vector3I adjustVector = startadjustVector + new Vector3I(step);
			while (trycount < 100 && mat == null)
			{
				step = dir * trycount;
				adjustVector = startadjustVector + new Vector3I(step);
				trycount++;
				mat = asteroid.asteroid.GetMaterial(adjustVector);
			}
			if (mat == null)
				adjustVector = startadjustVector;
			target = Vector3.Add(asteroid.asteroid.Position, adjustVector);
			//float adjust = Vector3.Distance(new Vector3Wrapper(0,0,0), adjustVector);
			Vector3Wrapper position = grid.Position;
			Vector3Wrapper Vector3Intercept = (Vector3Wrapper)FindInterceptVector(position, startSpeed, target, new Vector3Wrapper(0, 0, 0));

			float distance = Vector3.Distance(position,target) - slowDownDistance;
			if(distance > teleportDistance)
			{
				position = Vector3.Add(target, Vector3.Multiply(Vector3.Negate(Vector3.Normalize(Vector3Intercept)), teleportDistance));
				grid.Position = position;
				distance = Vector3.Distance(position, target) - slowDownDistance;
			}
			grid.Forward = Vector3.Normalize(Vector3Intercept);
			grid.LinearVelocity = Vector3Intercept;
			//LogManager.APILog.WriteLineAndConsole("Total Distance to target: " + distance.ToString());
			float timeToSlow =  distance / startSpeed;
			float timeToCollision = Vector3.Distance(Vector3.Subtract(target, Vector3.Multiply(Vector3.Normalize(Vector3Intercept), slowDownDistance)), target) / slowSpeed;

			ChatManager.Instance.SendPrivateChatMessage(steamid, "Estimated travel time: " + ((int)timeToSlow / 60).ToString() + " minutes and " + ((int)timeToSlow % 60).ToString() + " seconds. Enjoy your trip!");
			if (timeToSlow > 1)
				Thread.Sleep(1000);
			if ( Vector3.Distance(new Vector3Wrapper(0,0,0), grid.LinearVelocity) == 0)
			{
				timeToSlow += 1;
				//second attempt
				grid.Forward = Vector3.Normalize(Vector3Intercept);
				grid.LinearVelocity = Vector3Intercept;
			}
			int breakat = (int)timeToSlow*8;
			int breakcounter = 0;
			//calculate distance as we travel we can do these checks 4 times a second
			while (slowDownDistance < Vector3.Distance(grid.Position, target) - slowDownDistance)
			{
				breakcounter++;
				if (breakcounter % 80 * 30 == 0)
				{
					if(seat.PilotEntity != null)
						ChatManager.Instance.SendPrivateChatMessage(steamid, "Distance remaining: " + (Vector3.Distance(grid.Position, target) - slowDownDistance).ToString() + " meters.");
				}
				if(breakcounter > breakat) break;
				Thread.Sleep(250);
				if (grid == null) return;
			}
			if (seat.PilotEntity != null)
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, "Welcome to your destination, pod will attempt to land. If navigation calculations were off it will automatically stop the pod in " + ((int)timeToCollision*2).ToString() + " seconds. Have a nice day!");
				grid.LinearVelocity = Vector3.Multiply(Vector3.Normalize(Vector3Intercept), slowSpeed);
				Thread.Sleep((int)timeToCollision*2 * 1000);

				grid.LinearVelocity = new Vector3Wrapper(0, 0, 0);
				grid.AngularVelocity = new Vector3Wrapper(0, 0, 0);
			}
			else
				grid.Dispose();//clear it
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
					settings = obj;
					reader.Close();
					return;
				}
			}
			catch (Exception ex)
			{
				try { LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString()); }
				catch { Console.WriteLine("Could not load and write to log: " + ex.ToString()); }
				
			}
			try
			{
				if (File.Exists(DefaultLocation + "SE-Dropship-Settings.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEDropshipSettings));
					TextReader reader = new StreamReader(DefaultLocation + "SE-Dropship-Settings.xml");
					SEDropshipSettings obj = (SEDropshipSettings)x.Deserialize(reader);
					settings = obj;
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				try { LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString()); }
				catch { Console.WriteLine("Could not load and write to log: " + ex.ToString()); }
			}

		}

		#endregion
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

		public void OnCubeGridLoaded(CubeGridEntity grid)
		{

		}
		public void OnCubeGridDeleted(CubeGridEntity grid)
		{

		}
		public void OnCubeGridCreated(CubeGridEntity grid)
		{
			Thread T = new Thread(() => OnCubeGridDetected(grid));
			T.Start();
		}
		public void OnCubeGridMoved(CubeGridEntity grid)
		{

		}	
		#endregion

		#region "Chat Callbacks"

		public void saveXML(ChatManager.ChatEvent _event)
		{
			saveXML();
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.remoteUserId, "Dropship configuration saved.");
			}
			catch
			{
				//donothing
			}

		}
		public void loadXML(ChatManager.ChatEvent _event)
		{
			loadXML(false);
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.remoteUserId, "Dropship configuration loaded.");
			}
			catch
			{
				//donothing
			}
		}
		public void loadDefaults(ChatManager.ChatEvent _event)
		{
			loadXML(true);
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.remoteUserId, "Dropship configuration defaults loaded.");
			}
			catch
			{
				//donothing
			}
		}
		#endregion
		#endregion
	}
}
