using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		private bool m_deleteIfAbort = false;
		private int m_teleportDistance = 10000;
		private bool m_requireMagnesium = false;
		private bool m_requireVital = false;
		private string m_ignoreKeyword = "";
		private string m_arrivalMsg = "";
		private string m_bootupMsg = "";
		private double m_messageMult = 1.0d;
		private string m_whitelistKeyword = "";

		public int slowDownDistance
		{
			get { return m_slowDownDistance; }
			set { if (value > 128) m_slowDownDistance = value; else m_slowDownDistance = 128; }//safe minimum
		}

		public bool anyAsteroid
		{
			get { return m_anyAsteroid; }
			set { m_anyAsteroid = value; }
		}
		public int resolution
		{
			get { return m_resolution; }
			set { if (value > 0) m_resolution = value; }
		}
		public float slowSpeed
		{
			get { return m_slowSpeed; }
			set { if (value >= 0F) m_slowSpeed = value; }
		}
		public float startSpeed
		{
			get { return m_startSpeed; }
			set { if (value >= 0F) m_startSpeed = value; }
		}
		public int countdown
		{
			get { return m_countdown; }
			set { if (value >= 0) m_countdown = value; }
		}
		public bool deleteIfAbort
		{
			get { return m_deleteIfAbort; }
			set { m_deleteIfAbort = value; }
		}
		public int teleportDistance
		{
			get { return m_teleportDistance; }
			set { if (value >= 1000) m_teleportDistance = value; }
		}

		public bool requireMagnesium 
		{ 
			get { return m_requireMagnesium; }
			set { m_requireMagnesium = value; }
		}


		public bool requireVital 
		{
			get { return m_requireVital; }
			set { m_requireVital = value; }
		}

		public string ignoreKeyword 
		{
			get
			{
				return m_ignoreKeyword;
			}
			set
			{
				m_ignoreKeyword = value;
			}
		}

		public string arrivalMsg 
		{
			get { return m_arrivalMsg; } 
			set { m_arrivalMsg = value; } 
		}

		public string bootupMsg 
		{
			get { return m_bootupMsg; }
			set { m_bootupMsg = value; }
		}

		public double messageMult
		{
			get { return m_messageMult; }
			set { if(value >= 1.0d) m_messageMult = value; }
		}

		public string whitelistKeyword
		{
			get { return m_whitelistKeyword; }
			set { m_whitelistKeyword = value; }
		}
    }
}
