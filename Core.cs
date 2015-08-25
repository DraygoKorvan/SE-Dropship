using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml.Serialization;

using Sandbox.Common.ObjectBuilders;
using Sandbox;


using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;
using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;

using Sandbox.ModAPI;

using VRageMath;
using VRage.ModAPI;


namespace SEDropship
{
	/*struct MaterialPositionData
	{
		public Vector3 Sum;
		public int Count;
	}*/
	public class SEDropship : PluginBase, IChatEventHandler
	{
		
		#region "Attributes"
		public static NLog.Logger Log; 
		private Thread m_main;
		private SEDropshipSettings settings;
		private Random m_gen;
		private AsteroidCollection m_asteroids = new AsteroidCollection();
		private AsteroidCollection m_cache = new AsteroidCollection();
		//bool m_running = true;
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

			Thread.Sleep(1000);
			settings = new SEDropshipSettings();
			loadXML(false);
			m_gen = new Random((int)DateTime.UtcNow.ToBinary());
			//m_running = true;
			m_loading = true;
			//build asteroid list
			m_ignore.Clear();
			m_main = new Thread(mainloop);
			m_main.Start();
			m_main.Priority = ThreadPriority.BelowNormal;//lower priority to make room for other tasks if needed.

			MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
			MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
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
			get { return MySandboxGame.ConfigDedicated.LoadWorld + "\\"; }
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
		[Description("Plugin will ignore asteroid if this keyword exists in the asteroid filename. Note that if anyasteroid is not set, moon and ignore keywords are already ignored.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string ignoreKeyword
		{
			get { return settings.ignoreKeyword; }
			set { settings.ignoreKeyword = value; }
		}
		[Category("Asteroid")]
		[Description("Plugin will add asteroid if this keyword exists in the asteroid filename. This will override any other setting.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string whitelistKeyword
		{
			get { return settings.whitelistKeyword; }
			set { settings.whitelistKeyword = value; }
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
		[Category("SE Dropship")]
		[Description("Additional bootup message.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string bootupMsg
		{
			get { return settings.bootupMsg; }
			set { settings.bootupMsg = value; }
		}
		[Category("SE Dropship")]
		[Description("Additional arrival message.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string arrivalMsg
		{
			get { return settings.arrivalMsg; }
			set { settings.arrivalMsg = value; }
		}
		[Category("SE Dropship")]
		[Description("Message speed multiplier, must be greater than or equal to 1")]
		[Browsable(true)]
		[ReadOnly(false)]
		public double message_mult
		{
			get { return settings.messageMult; }
			set { settings.messageMult = value; }
		}

		#region "Debug"
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
			get { return m_debugging /* || SandboxGameAssemblyWrapper.IsDebugging */; }
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
		#endregion

		#region "Methods"

		#region "Core"
		private void mainloop()
		{
			//Thread.Sleep(resolution);
			
			m_asteroids.Clear();
			m_cache.Clear();
			//List<IMyVoxelMap> asteroids = new List<IMyVoxelMap>();

			do
			{
				Thread.Sleep(1000);
				try
				{
					if (m_loading == true)
					{
						Console.WriteLine("Attempting to get asteroid list.");


						HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
						try
						{

							MyAPIGateway.Entities.GetEntities(entities);
						}
						catch
						{
							Console.WriteLine("Server busy, asteroid list unavailible, skipping");
							//asteroids.Clear();
							m_loading = true;
						}
						foreach (IMyEntity entity in entities)
						{
							if (!(entity is IMyVoxelMap))
								continue;
							Console.WriteLine("Found voxelmap");
							IMyVoxelMap tmpasteroid = (IMyVoxelMap)entity;

							if (tmpasteroid.LocalAABB.Size.X > 120F)
							{
								m_cache.Add(new SeDropshipAsteroids(tmpasteroid));
								Console.WriteLine("Adding to valid asteroid list");
							}
								

						}
					}
				}
				catch 
				{
					Log.Info("Dropship - null reference trying again in 1 second.");
				}


			}
			while (m_cache.Count == 0);
			refresh();
		}

		private void refresh()
		{
			m_loading = true;
			m_asteroids.Clear();
			try
			{
				//var Cache = new MyStorageDataCache();
				foreach (SeDropshipAsteroids obj in m_cache)
				{
					//Thread.Sleep(1000);
					Console.WriteLine("Inventory started on asteroid");
					//var obj = new SeDropshipAsteroids(voxelmap);
					bool vitalmats = false;
					bool has_mag = false;
					string name = obj.Name;
					try
					{
						obj.Refresh();//refresh
					}
					catch
					{
						Console.WriteLine("Failure in inventory attempt..");
						continue;
					}
					if (obj.has_cobalt && obj.has_gold && obj.has_ice && obj.has_iron && obj.has_nickel && obj.has_silicon && obj.has_silver && obj.has_uranium)
						vitalmats = true;
					if (obj.has_magnesium)
						has_mag = true;

					bool add = true;
					if (obj.Name.Contains("moon") && !anyAsteroid)
						add = false;
					if (obj.Name.Contains("ignore"))
						add = false;
					if (ignoreKeyword != "")
					{
						if (obj.Name.Contains(ignoreKeyword))
							add = false;
					}
					if (!vitalmats && requireAllVitalMats)
						add = false;
					if (requireMagnesium && !has_mag)
						add = false;
					if (whitelistKeyword != "")
					{
						if (obj.Name.Contains(whitelistKeyword))
							add = true;
					}
					Console.WriteLine(String.Format("vitalmats: {0}, Magnesium: {1} Add: {2}", vitalmats.ToString(), has_mag.ToString(), add.ToString()));
					if (add)
					{
						m_asteroids.Add(obj);
					}

				}
				Console.WriteLine("Finished Loading.");
				m_loading = false;
			}
			catch (Exception ex)
			{
				Log.Error("Fatal error: " + ex.ToString());
				Log.Error("Stack: " + ex.StackTrace.ToString());
			}


			m_loading = false;
		}
		private Vector3D FindInterceptVector(Vector3D spawnOrigin, double dropshipspeed, Vector3D targetOrigin, Vector3D targetVel)
		{

			Vector3D dirToTarget = Vector3D.Normalize(targetOrigin - spawnOrigin);
			Vector3D targetVelOrth = Vector3D.Dot(targetVel, dirToTarget) * dirToTarget;
			Vector3D targetVelTang = targetVel - targetVelOrth;
			Vector3D shotVelTang = targetVelTang;
			double shotVelSpeed = shotVelTang.Length();
			if (shotVelSpeed > dropshipspeed)
			{
				return Vector3D.Multiply(targetVel, dropshipspeed);
			}
			else
			{
				double shotSpeedOrth = Math.Sqrt(dropshipspeed * dropshipspeed - shotVelSpeed * shotVelSpeed);
				Vector3D shotVelOrth = dirToTarget * shotSpeedOrth;
				return shotVelOrth + shotVelTang;
			}
		}

		private void OnEntityGridDetected(IMyEntity entity)
		{
			//runs in the main game thread!
			//Console.WriteLine("Entity Detected");
			if (entity is IMyCubeGrid)
			{
				//Console.WriteLine("GridDetected");
				IMyCubeGrid entitygrid = (IMyCubeGrid)entity;
				if (entitygrid.DisplayName.ToLower().Contains("dropship"))
				{
					//Console.WriteLine("Dropship detected");


					Thread breakout = new Thread(() => doDrop(entitygrid));
					breakout.Start();


				}
			}
		}
		public void doDrop(IMyCubeGrid grid)
		{

			//if(isdebugging)
				//Console.WriteLine("DoDrop called.");
			long Owner = 0;
			Thread.Sleep(1000);
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				if(grid.BigOwners.Count > 0)
					Owner = grid.BigOwners.First();
			});
			while (Loading) Thread.Sleep(1000);
			//find target

			SeDropshipAsteroids asteroid = m_asteroids.First();
			if(anyAsteroid)
			{
				asteroid = m_asteroids.ElementAt(m_gen.Next(m_asteroids.Count));
			}
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				grid.Physics.Activate();//fixes a bug...
			});
			//store target position.
			Vector3D target = asteroid.center;

			List<ulong> steamlist = ServerNetworkManager.Instance.GetConnectedPlayers();
			ulong steamid = 0;
			foreach( ulong steam_id in steamlist)
			{
				List<long> playerids =  PlayerMap.Instance.GetPlayerIdsFromSteamId(steam_id);
				foreach (long playerid in playerids)
				{
					//Console.WriteLine("Connected playerid:" + playerid.ToString());
					if(playerid == Owner)
					{
						steamid = steam_id;
						//Console.WriteLine("Found match" + steamid.ToString());
					}
				}
			}
			if (m_asteroids.Count == 0)
			{
				Log.Info("No asteroids, aborting drop");
				ChatManager.Instance.SendPrivateChatMessage(steamid, "Error: No valid asteroids exist within range, aborting drop.");
                return;
			}
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Dropship booting up, please stay in your seat for insertion.");
			Thread.Sleep((int)(2000 * message_mult));
			ChatManager.Instance.SendPrivateChatMessage(steamid, "If you exited your ship please return to the passenger seat before the countdown finishes.");
			Thread.Sleep((int)(5000 * message_mult));
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Dropship Sequence Initiated, please remain seated. Exiting your seat will abort automatic insertion.");
			Thread.Sleep((int)(2000 * message_mult));
			ChatManager.Instance.SendPrivateChatMessage(steamid, "Beginning Insertion Sequence.");
			Thread.Sleep((int)(1000 * message_mult));	
			for ( int count = countdown; count > 0; count--)
			{
				ChatManager.Instance.SendPrivateChatMessage(steamid, count.ToString() + ".");
				Thread.Sleep(1000);
			}

			ChatManager.Instance.SendPrivateChatMessage(steamid, "Insertion Sequence initiated.");

			if (bootupMsg != "")
			ChatManager.Instance.SendPrivateChatMessage(steamid, bootupMsg);
			Vector3D position = Vector3D.Zero;
            SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				position = grid.Physics.CenterOfMassWorld;
			});
			
			
			Vector3D Vector3Intercept = FindInterceptVector(position, startSpeed, target, new Vector3D(0, 0, 0));
			int slowDist = slowDownDistance + asteroid.halfextent;
            float distance = Vector3.Distance(position,target) - slowDist;
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				if (distance > teleportDistance)
				{
					position = Vector3.Add(target, Vector3.Multiply(Vector3.Negate(Vector3.Normalize(Vector3Intercept)), teleportDistance));
					grid.SetPosition(position);
					distance = Vector3.Distance(position, target) - slowDist;
				}
				//grid.Physics.
				var matrix = grid.Physics.GetWorldMatrix();
				matrix.Forward = Vector3.Normalize(Vector3Intercept);
				grid.SetWorldMatrix(matrix);
				grid.Physics.LinearVelocity = (Vector3)Vector3Intercept;
			});
			//LogManager.APILog.WriteLineAndConsole("Total Distance to target: " + distance.ToString());
			float timeToSlow =  distance / startSpeed;
			float timeToCollision = Vector3.Distance(Vector3.Subtract(target, Vector3.Multiply(Vector3.Normalize(Vector3Intercept), slowDist)), target) / slowSpeed;

			ChatManager.Instance.SendPrivateChatMessage(steamid, "Estimated travel time: " + ((int)timeToSlow / 60).ToString() + " minutes and " + ((int)timeToSlow % 60).ToString() + " seconds. Enjoy your trip!");
			if (timeToSlow > 1)
				Thread.Sleep(1000);

			int breakat = (int)timeToSlow*8;
			int breakcounter = 0;
			//calculate distance as we travel we can do these checks 4 times a second
			while (slowDownDistance < Vector3D.Distance(grid.GetPosition(), target) - slowDist)
			{
				breakcounter++;
				if (breakcounter % 80 == 0)
				{
					//if(seat.Pilot != null)
					ChatManager.Instance.SendPrivateChatMessage(steamid, "Distance remaining: " + (Vector3D.Distance(grid.GetPosition(), target) - slowDist).ToString() + " meters.");
				}
				if(breakcounter > breakat) break;
				Thread.Sleep(250);
				if (grid == null) return;
			}

			ChatManager.Instance.SendPrivateChatMessage(steamid, "Welcome to your destination, pod will attempt to land. If navigation calculations were off it will automatically stop the pod in " + ((int)timeToCollision*2).ToString() + " seconds. Have a nice day!");
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				grid.Physics.LinearVelocity = (Vector3)Vector3D.Multiply(Vector3D.Normalize(Vector3Intercept), slowSpeed);
			});
			Thread.Sleep(1000);

			if (arrivalMsg != "")
				ChatManager.Instance.SendPrivateChatMessage(steamid, arrivalMsg);

			Thread.Sleep((int)timeToCollision * 2 * 1000);
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				grid.Physics.LinearVelocity = new Vector3(0, 0, 0);
				grid.Physics.AngularVelocity = new Vector3(0, 0, 0);
				//grid.SyncObject.UpdatesOnlyOnServer = updatesonserver;
			});


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
				try { Log.Warn("Could not load configuration: " + ex.ToString()); }
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
				try { Log.Warn("Could not load configuration: " + ex.ToString()); }
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
			//m_running = false;
			saveXML();
			return;
		}


		public void OnEntityAdd(IMyEntity entity)
		{
			OnEntityGridDetected(entity);

		}

		public void OnChatReceived(ChatManager.ChatEvent chatEvent)
		{
			if (chatEvent.Message[0] != '/')
				return;
			HandleChatMessage(chatEvent);
		}

		public void OnChatSent(ChatManager.ChatEvent chatEvent)
		{
			return;
		}



		#endregion

		#region "Chat Callbacks"

		private void HandleChatMessage(ChatManager.ChatEvent _event)
		{
			string[] words = _event.Message.Split(' ');
			words[0] = words[0].ToLower();
			bool isadmin = (PlayerManager.Instance.IsUserAdmin(_event.SourceUserId) || _event.SourceUserId == 0);
			if (words[0] == "/ds-save" && isadmin)
				commandSaveXML(_event);
			if (words[0] == "/ds-load" && isadmin)
				commandLoadXML(_event);
			if (words[0] == "/ds-loaddefaults" && isadmin)
				commandLoadDefaults(_event);
			if (words[0] == "/ds-refresh" && isadmin)
				commandRefresh(_event);
		}

		
		public void commandSaveXML(ChatManager.ChatEvent _event)
		{
			saveXML();
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.SourceUserId, "Dropship configuration saved.");
			}
			catch
			{
				//donothing
			}

		}
		public void commandLoadXML(ChatManager.ChatEvent _event)
		{
			loadXML(false);
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.SourceUserId, "Dropship configuration loaded.");
			}
			catch
			{
				//donothing
			}
		}
		public void commandLoadDefaults(ChatManager.ChatEvent _event)
		{
			loadXML(true);
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.SourceUserId, "Dropship configuration defaults loaded.");
			}
			catch
			{
				//donothing
			}
		}
		public void commandRefresh(ChatManager.ChatEvent _event)
		{
			Thread T = new Thread(refresh);
			T.Start();
			try
			{
				ChatManager.Instance.SendPrivateChatMessage(_event.SourceUserId, "Refreshing Asteroid Collection, this may take a while.");
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
