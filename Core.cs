using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using Sandbox;
using SteamSDK;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;
using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;

using Sandbox.Game.World;
using Sandbox.ModAPI;

using VRageMath;
using VRage.ModAPI;


namespace SEDropship
{
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
	
			m_main = new Thread(mainloop);
			m_main.Start();
			m_main.Priority = ThreadPriority.BelowNormal;//lower priority to make room for other tasks if needed.

			MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
			MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
			pAM = new PersonalAsteroidManager();

			pAM.load();

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
		[Description("Requires target asteroid to have Iron, Nickel, Cobalt, Silver, Gold, Platinum, and Silicon.")]
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
		[Category("Asteroid")]
		[Description("Requires target asteroid to have Uranium.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool requireUranium
		{
			get { return settings.requireUranium; }
			set { settings.requireUranium = value; }
		}
		[Category("Asteroid")]
		[Description("Requires target asteroid to have Ice.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool requireIce
		{
			get { return settings.requireIce; }
			set { settings.requireIce = value; }
		}
		[Category("Asteroid")]
		[Description("Requires target asteroid to have Silver.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool requireSilver
		{
			get { return settings.requireSilver; }
			set { settings.requireSilver = value; }
		}
		[Category("Asteroid")]
		[Description("Personal Roid System - Warning - could have a heavy system resource requirement!")]
		[Browsable(true)]
		[ReadOnly(false)]//disabled
		public bool personalRoidEnabled
		{
			get
			{
				return settings.personalRoid;
				//return false;
			}
			set
			{
				//settings.personalRoid = false; //disabled
				settings.personalRoid = value;
			}
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

		private PersonalAsteroidManager pAM
		{
			get;
			set;
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


			Thread.Sleep(1000);
			try
			{
				if (m_loading == true)
				{
					//Console.WriteLine("Attempting to get asteroid list.");


					HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					MyAPIGateway.Entities.GetEntities(entities);
					foreach (IMyEntity entity in entities)
					{
						if (!(entity is IMyVoxelMap))
							continue;
						//Console.WriteLine("Found voxelmap");
						IMyVoxelMap tmpasteroid = (IMyVoxelMap)entity;

						if (tmpasteroid.LocalAABB.Size.X > 120F)
						{
							m_cache.Add(new SeDropshipAsteroids(tmpasteroid));
							Console.WriteLine("Adding to valid asteroid list");
						}
						else
						{
							Console.WriteLine("Asteroid too small, skipping.");
						}
								

					}
				}
			}
			catch 
			{
				Log.Info("Dropship - null reference trying again in 1 second.");
			}

			refresh();
		}

		private void refresh()
		{
			m_loading = true;
			m_asteroids.Clear();
			try
			{
				foreach (SeDropshipAsteroids obj in m_cache)
				{
					bool vitalmats = false;
					bool has_mag = false;
					bool has_ura = false;
					bool has_ice = false;
					bool has_silver = false;
					string name = obj.Name;
					if (obj.Name.Contains("p-Roid-"))
						continue;

					Console.WriteLine("Inventory started on asteroid");
					try
					{
						
						obj.Refresh();//refresh
					}
					catch
					{
						Console.WriteLine("Failure in inventory attempt..");
						continue;
					}
					//uranium is not required
					if (obj.has_cobalt && obj.has_gold && obj.has_iron && obj.has_nickel && obj.has_silicon && obj.has_platinum)
						vitalmats = true;
					if (obj.has_magnesium)
						has_mag = true;
					if (obj.has_uranium)
						has_ura = true;
					if (obj.has_ice)
						has_ice = true;
					if (obj.has_silver)
						has_silver = true;
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
					if (requireUranium && !has_ura)
						add = false;
					if (requireIce && !has_ice)
						add = false;
					if (requireSilver && !has_silver)
						add = false;
					if (whitelistKeyword != "")
					{
						if (obj.Name.Contains(whitelistKeyword))
							add = true;
					}
					Console.WriteLine(String.Format("vitalmats: {0}, Magnesium: {1}, Uranium: {3}, Add: {2}", vitalmats.ToString(), has_mag.ToString(), add.ToString(), has_ura.ToString()));
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
			long Owner = 0;
			Thread.Sleep(1000);
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				if(grid.BigOwners.Count > 0)
					Owner = grid.BigOwners.First();
			});
			List<ulong> steamlist = ServerNetworkManager.Instance.GetConnectedPlayers();
			ulong steamid = 0;
			foreach (ulong steam_id in steamlist)
			{
				List<long> playerids = PlayerMap.Instance.GetPlayerIdsFromSteamId(steam_id);
				foreach (long playerid in playerids)
				{
					if (playerid == Owner)
					{
						steamid = steam_id;
					}
				}
			}
			
			while (Loading && !personalRoidEnabled) Thread.Sleep(1000);
			//find target
			//SeDropshipAsteroids asteroid;
			Vector3D target = Vector3D.Zero;
			int halfextent = 0;
			if (personalRoidEnabled)
			{
				target = pAM.targetpos(steamid);
				halfextent = pAM.halfextent(steamid);
			}
			else
			{
				if( m_asteroids.Count  > 0)
                {
					var asteroid = m_asteroids.First();
					if (anyAsteroid)
					{
						asteroid = m_asteroids.ElementAt(m_gen.Next(m_asteroids.Count));
					}
					target = asteroid.center;
					halfextent = asteroid.halfextent;
				}
				else
				{

					Log.Info("No asteroids, aborting drop");
					ChatManager.Instance.SendPrivateChatMessage(steamid, "Error: No valid asteroids exist within range sending to 0,0,0.");
					

				}

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
			Console.WriteLine("getting center of mass");
			position = grid.GetPosition();

			Console.WriteLine("calculating intercept");
			Vector3D Vector3Intercept = FindInterceptVector(position, startSpeed, target, new Vector3D(0, 0, 0));
			int slowDist = slowDownDistance + halfextent;
            float distance = Vector3.Distance(position,target) - slowDist;
			//Console.WriteLine("moving ship!");
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				//Console.WriteLine("In engine");
				if (distance > teleportDistance)
				{
					//Console.WriteLine("teleporting");
					position = Vector3.Add(target, Vector3.Multiply(Vector3.Negate(Vector3.Normalize(Vector3Intercept)), teleportDistance));
					//Console.WriteLine("set grid pos");
					try
					{
						grid.SetPosition(position);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
						Console.WriteLine(ex.StackTrace.ToString());

					}
				}
			});


			Thread.Sleep(1000);
				
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				try
				{
					position = grid.GetPosition();
					distance = Vector3.Distance(position, target) - slowDist;
					var matrix = grid.Physics.GetWorldMatrix();
					matrix.Forward = Vector3D.Normalize(Vector3Intercept);
					grid.SetWorldMatrix(matrix);
					grid.Physics.LinearVelocity = (Vector3)Vector3Intercept;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}

			});
			Console.WriteLine("out engine");
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
				try
				{
					grid.Physics.LinearVelocity = (Vector3)Vector3D.Multiply(Vector3D.Normalize(Vector3Intercept), slowSpeed);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
				
			});
			Thread.Sleep(1000);

			if (arrivalMsg != "")
				ChatManager.Instance.SendPrivateChatMessage(steamid, arrivalMsg);

			Thread.Sleep((int)timeToCollision * 2 * 1000);
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				try
				{
					grid.Physics.LinearVelocity = new Vector3(0, 0, 0);
					grid.Physics.AngularVelocity = new Vector3(0, 0, 0);
				}
				catch (Exception ex)
				{
					Console.Write(ex.ToString());
				}
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

			MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
			saveXML();
			return;
		}


		public void OnEntityAdd(IMyEntity entity)
		{
			OnEntityGridDetected(entity);
			/*if (entity is IMyVoxelMap)
			{
				IMyVoxelMap tmpasteroid = (IMyVoxelMap)entity;
				Console.WriteLine(string.Format("N:{0} R:{1} E:{2}", tmpasteroid.StorageName.ToString(), Math.Floor(tmpasteroid.LocalVolume.Radius / 2), tmpasteroid.LocalAABB.HalfExtents.AbsMax().ToString()));
			}*/
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

		private void OnClientLeft(ulong arg1, ChatMemberStateChangeEnum arg2)
		{

		}

		private void OnClientJoined(ulong obj)
		{
			pAM.loadClient(obj);
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
			/*if (words[0] == "/ds-test")
			{
				Console.WriteLine("beginning test");
				commandTest(_event);
				
            }*/
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
		public void commandTest(ChatManager.ChatEvent _event)
		{
			//Thread T = new Thread(Experiment);
			//T.Start();
		}

		/*private void Experiment()
		{
			int i = 0;
			var pos = new Vector3D(10000, 10000, 10000);
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				MyWorldGenerator.AddAsteroidPrefab("AsteroidBase2", pos + new Vector3D(0,0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("AsteroidSpaceStation", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("Barths_moon_base", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("barths_moon_camp", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("Bioresearch", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("Chinese_Corridor_Tunnel_256x256x256", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("EacPrisonAsteroid", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("EngineersOutpost", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("hopebase512", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("Junkyard_RaceAsteroid_256x256x256", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("JunkYardToxic_128x128x128", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("PirateBaseStaticAsteroid_A_5000m_1", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("PirateBaseStaticAsteroid_A_5000m_2", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("Russian_Transmitter_2", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("ScratchedBoulder_128x128x128", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("small_overhang_flat", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("Small_Pirate_Base_Asteroid", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("TorusWithManyTunnels_256x128x256", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("TorusWithSmallTunnel_256x128x256", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("VangelisBase", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
				MyWorldGenerator.AddAsteroidPrefab("VerticalIslandStorySector_128x256x128", pos + new Vector3D(0, 0, i++ * 10000), string.Format("p-Roid-{0}", i++));
			});*/
			/*
N:p-Roid-1 R:443 E:256
N:p-Roid-3 R:221 E:128
N:p-Roid-5 R:221 E:128
N:p-Roid-7 R:221 E:128
N:p-Roid-9 R:221 E:128
N:p-Roid-11 R:221 E:128
N:p-Roid-13 R:443 E:256
N:p-Roid-15 R:221 E:128
N:p-Roid-17 R:443 E:256
N:p-Roid-19 R:221 E:128
N:p-Roid-21 R:110 E:64
N:p-Roid-23 R:443 E:256
N:p-Roid-25 R:443 E:256
N:p-Roid-27 R:221 E:128
N:p-Roid-29 R:110 E:64
N:p-Roid-31 R:110 E:64
N:p-Roid-33 R:110 E:64
N:p-Roid-35 R:221 E:128
N:p-Roid-37 R:221 E:128
N:p-Roid-39 R:221 E:128
N:p-Roid-41 R:221 E:128
	*/

		}
		#endregion
		#endregion

		new public Guid Id
		{
			get
			{
				GuidAttribute guidAttr = (GuidAttribute)typeof(SEDropship).Assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
				return new Guid(guidAttr.Value);
			}
		}

		new public string Name
		{
			get
			{
				return "SE-Dropship";
			}
		}

		new public Version Version
		{
			get
			{
				return typeof( SEDropship ).Assembly.GetName().Version;
			}
		}
	}
}
