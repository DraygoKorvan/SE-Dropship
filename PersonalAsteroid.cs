using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

using SEModAPIInternal.API.Common;

using Sandbox.Game.World;
using VRage.Utils;

namespace SEDropship
{
	[Serializable()]
	public class PersonalAsteroid
	{
		public Vector3I field;
		public ulong user;
		private Random m_rand;
		private static readonly Dictionary<string, int> LargePrefabs
			= new Dictionary<string, int>
			{
				
				{ "EacPrisonAsteroid", 222 },
				{ "hopebase512", 222 },
				{ "PirateBaseStaticAsteroid_A_5000m_1", 222 },
				{ "PirateBaseStaticAsteroid_A_5000m_2", 222 }

			};

		private static readonly Dictionary<string, int> SmallPrefabs
			= new Dictionary<string, int>
			{
				{ "AsteroidBase2", 111 },
				{ "AsteroidSpaceStation", 111 },
				{ "Barths_moon_base", 111 },
				{ "barths_moon_camp", 111 },
				{ "Bioresearch", 111 },
				{ "Chinese_Corridor_Tunnel_256x256x256", 111 },
				{ "EngineersOutpost", 111 },
				{ "Junkyard_RaceAsteroid_256x256x256", 111 },
				{ "Russian_Transmitter_2", 111 },
				{ "TorusWithManyTunnels_256x128x256", 111 },
				{ "TorusWithSmallTunnel_256x128x256", 111 },
				{ "VangelisBase", 111 },
				{ "VerticalIslandStorySector_128x256x128", 111 },
				{ "JunkYardToxic_128x128x128", 55 },
				{ "ScratchedBoulder_128x128x128", 55 },
				{ "small_overhang_flat", 55 },
				{ "Small_Pirate_Base_Asteroid", 55 }

			};

		public Vector3D center { get; set; }
		public int halfextent { get; set; }
		internal PersonalAsteroid()
		{

		}
		public PersonalAsteroid(Vector3I newField, ulong uid)
		{
			m_rand = new Random((int)DateTime.UtcNow.ToBinary());
			user = uid;
			this.field = newField;
			generateField();
		}

		private void generateField()
		{
			int i = 0;
			Console.WriteLine("Generating field");
			//spawn home asteroid
			int pick = m_rand.Next(LargePrefabs.Count);
			Vector3D pos = field;
			//pos = pos * SEDropshipSettings.FieldSize;//home position
			Console.WriteLine("Setting Home Position");
			pos.X = (pos.X * SEDropshipSettings.FieldSize) + (m_rand.NextDouble() * 40000 - 20000);
			pos.Y = (pos.Y * SEDropshipSettings.FieldSize) + (m_rand.NextDouble() * 40000 - 20000);
			pos.Z = (pos.Z * SEDropshipSettings.FieldSize) + (m_rand.NextDouble() * 40000 - 20000);
			Console.WriteLine(pos.ToString());
			center = pos;
			halfextent = LargePrefabs.ElementAt(pick).Value;
			pos.X -= halfextent;
			pos.Y -= halfextent;
			pos.Z -= halfextent;
			Console.WriteLine("Spawning home roid");
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				try
				{
					MyWorldGenerator.AddAsteroidPrefab(LargePrefabs.First().Key, pos, string.Format("p-Roid-{0}_{1}", i, user));
				}
				catch (Exception ex)
				{
					MyLog.Default.WriteLineAndConsole(ex.ToString());
					MyLog.Default.WriteLineAndConsole(ex.StackTrace.ToString());
					throw ex;
				}
			});


			//spawn second large asteroid
			i++;
			var randvector = Vector3.Multiply(Vector3.Normalize(new Vector3D(m_rand.NextDouble() - 0.5, m_rand.NextDouble() - 0.5, m_rand.NextDouble() - 0.5)), 1500);
			var newpos = pos + randvector;
			pick = m_rand.Next(SmallPrefabs.Count);
			string _roid = SmallPrefabs.ElementAt(pick).Key;
			int _halfextent = SmallPrefabs[_roid];
			newpos.X -= _halfextent;
			newpos.Y -= _halfextent;
			newpos.Z -= _halfextent;
			Console.WriteLine("spawning second roid");
			SandboxGameAssemblyWrapper.Instance.GameAction(() =>
			{
				try
				{
					MyWorldGenerator.AddAsteroidPrefab(_roid, newpos, string.Format("p-Roid-{0}_{1}", i, user));
				}
				catch (Exception ex)
				{
					MyLog.Default.WriteLineAndConsole(ex.ToString());
					MyLog.Default.WriteLineAndConsole(ex.StackTrace.ToString());
					throw ex;
				}
			});

			//spawn small asteroids
			while (i < 5)
			{
				i++;
				randvector = Vector3.Multiply(Vector3.Normalize(new Vector3D(m_rand.NextDouble()-0.5, m_rand.NextDouble() - 0.5, m_rand.NextDouble() - 0.5)), 1500 + i*500);
				newpos = pos + randvector;
				pick = m_rand.Next(LargePrefabs.Count);
				_roid = SmallPrefabs.ElementAt(pick).Key;
				_halfextent = SmallPrefabs[_roid];
				newpos.X -= _halfextent;
				newpos.Y -= _halfextent;
				newpos.Z -= _halfextent;
				Console.WriteLine("Spawning " + i.ToString() + " + 2 roid.");
				SandboxGameAssemblyWrapper.Instance.GameAction(() =>
				{
					try
					{
						MyWorldGenerator.AddAsteroidPrefab(_roid, newpos, string.Format("p-Roid-{0}_{1}", i, user));
					}
					catch (Exception ex)
					{
						MyLog.Default.WriteLineAndConsole(ex.ToString());
						MyLog.Default.WriteLineAndConsole(ex.StackTrace.ToString());
						throw ex;
					}
				});
			}
			


		}


	}
}
