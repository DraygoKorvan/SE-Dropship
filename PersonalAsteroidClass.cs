using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;

using VRageMath;
using Sandbox;
using Sandbox.ModAPI;

namespace SEDropship
{
	[Serializable()]
	public class PersonalAsteroidManager
	{
		[NonSerialized]
		private Dictionary<ulong, PersonalAsteroid> m_cache = new Dictionary<ulong, PersonalAsteroid>();
		//private List<ulong> m_clients = new List<ulong>();
		[NonSerialized]
		private long m_pos = 2;

		internal PersonalAsteroidManager() { }
		public List<PersonalWrapper> m_savedata
		{
			get; set;
		}
		public long pos
		{
			get
			{
				return m_pos;

			}
			set
			{
				m_pos = value;
			}
		}
		internal void load()
		{
			try
			{
				m_savedata = new List<PersonalWrapper>();
				if (File.Exists(Location + "SE-Personal-Asteroid.xml"))
				{

					XmlSerializer x = new XmlSerializer(typeof(PersonalAsteroidManager));
					TextReader reader = new StreamReader(Location + "SE-Personal-Asteroid.xml");
					PersonalAsteroidManager obj = (PersonalAsteroidManager)x.Deserialize(reader);
					pos = obj.pos;
					m_savedata = obj.m_savedata;
					reader.Close();
					sync();

					return;
				}

			}
			catch (Exception)
			{
				//do nothin
				Console.WriteLine("Exception thrown while loading.");
			}
		}

		private void sync()
		{
			foreach( var wrapper in m_savedata)
			{
				m_cache.Add(wrapper.id, wrapper.roid);
			}
		}

		internal void loadClient(ulong obj)
		{
	
			//save();
		}

		private void save()
		{
			//save settings and trigger world save
			XmlSerializer x = new XmlSerializer(typeof(PersonalAsteroidManager));
			TextWriter writer = new StreamWriter(Location + "SE-Personal-Asteroid.xml");
			x.Serialize(writer, this);
			writer.Close();
			Console.WriteLine("Saving");
			MyAPIGateway.Session.Save();
		}



		public Vector3I getNewFieldLocation()
		{
			m_pos++;

			Vector3I val = Vector3I.Zero;
			val.Z = (int)(m_pos % 3)-1;
			long div = m_pos / 3;
			long edge = (long)Math.Floor(Math.Sqrt(div));
			if ((edge % 2) == 0)
			{
				edge--;
			}
			edge = edge / 2;
			edge++;

			//now we have our edge
			val.X = (int)-edge;
			val.Y = (int)-edge;

			int pos = (int)(div - ((edge*2-1) * (edge * 2 - 1)));
			
		
			switch (pos % 4)
			{
				case 0:
					val.X += pos / 4;
					val.Y *= -1;
					break;
				case 1:
					val.X *= -1;
					val.Y *= -1;
					val.Y -= pos / 4;
					break;
				case 2:
					val.X *= -1;
					val.X -= pos / 4;
					val.Y += 0;
					break;
				case 3:
					val.X += 0;
					val.Y += pos / 4 ;
					break;
			}

			return val;
		}

		internal Vector3D targetpos(ulong steamid)
		{
			if (!m_cache.ContainsKey(steamid))
			{
				createEntry(steamid);
			}
			return m_cache[steamid].center;
		}

		private void createEntry(ulong steamid)
		{
			Console.WriteLine("Creating roid");
			var roid = new PersonalAsteroid(getNewFieldLocation(), steamid);
			Console.WriteLine("Adding to m_cache");
			m_cache.Add(steamid, roid);
			Console.WriteLine("Adding to savedata");
			m_savedata.Add(new PersonalWrapper(steamid, roid));
			Console.WriteLine("Initiating save");
			save();
		}

		internal int halfextent(ulong steamid)
		{
			if (!m_cache.ContainsKey(steamid))
			{
				createEntry(steamid);
				save();
			}
			return m_cache[steamid].halfextent;
		}

		public string DefaultLocation
		{
			get { return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\"; }

		}

		public string Location
		{
			get { return MySandboxGame.ConfigDedicated.LoadWorld + "\\"; }
		}


	}
	[Serializable()]
	public class PersonalWrapper
	{
		public PersonalWrapper(ulong steamid, PersonalAsteroid roid)
		{
			id = steamid;
			this.roid = roid;
		}
		public PersonalWrapper() { }

		public ulong id { get; set; }
		public PersonalAsteroid roid { get; set; }
	}
}
