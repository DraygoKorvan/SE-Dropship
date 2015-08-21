using System;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

//using SEModAPIInternal.API.Entity.Sector.SectorObject;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Voxels;

using VRageMath;
using VRage.Voxels;


namespace SEDropship
{
	public class AsteroidCollection : CollectionBase, ICustomTypeDescriptor, IList
	{
		public void Add( SeDropshipAsteroids roid )
		{
			this.List.Add( roid );
		} 
		public void Remove( SeDropshipAsteroids roid )
		{ 
			this.List.Remove( roid);
		} 
		public SeDropshipAsteroids this[ int index ] 
		{ 
			get
			{
				return (SeDropshipAsteroids)this.List[index];
			}
		}
		public SeDropshipAsteroids First()
		{
			return this[0];
		}
		public SeDropshipAsteroids ElementAt(int index)
		{
			return this[index];
		}
		public String GetClassName()
		{
			return TypeDescriptor.GetClassName(this,true);
		}
  
		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this,true);
		}
 
		public String GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}
  
		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}
  
		public EventDescriptor GetDefaultEvent() 
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}
 
		public PropertyDescriptor GetDefaultProperty() 
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		public object GetEditor(Type editorBaseType) 
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}
  
		public EventDescriptorCollection GetEvents(Attribute[] attributes) 
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}
  
		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}
  
		public object GetPropertyOwner(PropertyDescriptor pd) 
		{
			return this;
		}

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			return GetProperties();
		}

		public PropertyDescriptorCollection GetProperties()
		{
			// Create a collection object to hold property descriptors
			PropertyDescriptorCollection pds = new PropertyDescriptorCollection(null);
  
			for( int i=0; i<this.List.Count; i++ )
			{
				AsteroidCollectionPropertyDescriptor pd = new AsteroidCollectionPropertyDescriptor(this,i);
				pds.Add(pd);
			}
			// return the property descriptor collection
			return pds;
		}
	}

	public class AsteroidCollectionPropertyDescriptor : PropertyDescriptor
	{
		private AsteroidCollection collection = null;
		private int index = -1;

		public AsteroidCollectionPropertyDescriptor(AsteroidCollection coll, int idx)
			: base("#" + idx.ToString(), null)
		{
			this.collection = coll;
			this.index = idx;
		} 

		public override AttributeCollection Attributes
		{
			get 
			{ 
				return new AttributeCollection(null);
			}
		}

		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get 
			{ 
				return this.collection.GetType();
			}
		}

		public override string DisplayName
		{
			get 
			{
				SeDropshipAsteroids emp = this.collection[index];
				return emp.asteroid.GetFriendlyName();
			}
		}

		public override string Description
		{
			get
			{
				SeDropshipAsteroids roid = this.collection[index];
				StringBuilder sb = new StringBuilder();
				sb.Append(roid.asteroid.GetFriendlyName());
				sb.Append(", X:");
				sb.Append(roid.x);
				sb.Append(", Y:");
				sb.Append(roid.y);
				sb.Append(", Z:");
				sb.Append(roid.z);

				return sb.ToString();
			}
		}

		public override object GetValue(object component)
		{
			return this.collection[index];
		}

		public override bool IsReadOnly
		{
			get { return true;  }
		}

		public override string Name
		{
			get { return "#"+index.ToString(); }
		}

		public override Type PropertyType
		{
			get { return this.collection[index].GetType(); }
		}

		public override void ResetValue(object component) {}

		public override bool ShouldSerializeValue(object component)
		{
			return true;
		}

		public override void SetValue(object component, object value)
		{

		}
	}
	internal class AsteroidsConverter : ExpandableObjectConverter
	{
		public override object ConvertTo(ITypeDescriptorContext context, 
								 System.Globalization.CultureInfo culture, 
								 object value, Type destType )
		{
			if (destType == typeof(string) && value is SeDropshipAsteroids)
			{
				SeDropshipAsteroids asteroids = (SeDropshipAsteroids)value;
				return asteroids.asteroid.GetFriendlyName();
			}
			return base.ConvertTo(context,culture,value,destType);
		}
	}

	[Serializable()]
	[TypeConverter(typeof(AsteroidsConverter))]
	public class SeDropshipAsteroids
	{
		private IMyVoxelMap m_asteroid;
		private double m_sizex = 50;
		private double m_sizey = 50;
		private double m_sizez = 50;
		private Dictionary<MyVoxelMaterialDefinition, float> m_materialTotals = new Dictionary<MyVoxelMaterialDefinition, float>();
		private MyStorageDataCache m_cache;
		private List<MyVoxelMaterialDefinition> m_Defs = new List<MyVoxelMaterialDefinition>();
		private Vector3D m_position;
		private string m_name;

		private static MyVoxelMaterialDefinition m_irondef = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Iron_01");
		private static MyVoxelMaterialDefinition m_irondef2 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Iron_02");
		private static MyVoxelMaterialDefinition m_nickeldef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Nickel_01");
		private static MyVoxelMaterialDefinition m_cobaltdef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Cobalt_01");
		private static MyVoxelMaterialDefinition m_magnesiumdef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Magnesium_01");
		private static MyVoxelMaterialDefinition m_silicondef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Silicon_01");
		private static MyVoxelMaterialDefinition m_silverdef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Silver_01");
		private static MyVoxelMaterialDefinition m_golddef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Gold_01");
		private static MyVoxelMaterialDefinition m_platinumdef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Platinum_01");
		private static MyVoxelMaterialDefinition m_uraniumdef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Uraninite_01");
		private static MyVoxelMaterialDefinition m_icedef1 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Ice_01");
		private static MyVoxelMaterialDefinition m_icedef2 = Sandbox.Definitions.MyDefinitionManager.Static.GetVoxelMaterialDefinition("Ice_02");




		public SeDropshipAsteroids(IMyVoxelMap asteroid)
		{
			m_asteroid = asteroid;
			m_name = m_asteroid.StorageName.ToString();
            calculateSize();
		}

		private void calculateSize()
		{
			try
			{
				m_position = m_asteroid.PositionLeftBottomCorner - m_asteroid.Physics.Center;
				m_sizex = m_position.X;
				m_sizey = m_position.Y;
				m_sizez = m_position.Z;
			}
			catch (Exception)
			{
				m_sizex = 50;
				m_sizey = 50;
				m_sizez = 50;
				return;
			}

		}
		public void Refresh()
		{
			m_cache = new MyStorageDataCache();
			Vector3I size = m_asteroid.Storage.Size;
			m_cache.Resize(size);
			m_asteroid.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, 0, Vector3I.Zero, size - 1);
			//voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, Vector3I.Zero, size - 1);
			//			});

			foreach (byte materialIndex in m_cache.Data)
			{
				try
				{
					MyVoxelMaterialDefinition material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIndex);
					if (material == null)
						continue;

					if (!m_materialTotals.ContainsKey(material))
					{
						m_materialTotals.Add(material, 1);
						m_Defs.Add(material);
					}
					else
						m_materialTotals[material]++;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}

		}

		[Browsable(true)]
		[ReadOnly(true)]
		public IMyVoxelMap asteroid
		{
			get { return m_asteroid; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public double x
		{
			get { return m_sizex; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public double y
		{
			get { return m_sizey; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public double z
		{
			get { return m_sizez; }

		}
		[Browsable(true)]
		[ReadOnly(true)]
		public string Name
		{
			get
			{
				if (m_name != null)
					return m_name;
				if (m_asteroid != null)
					return m_name = m_asteroid.StorageName.ToString();
				return null;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_iron
		{
			get 
			{
				if (m_cache != null)
					return m_Defs.Contains(m_irondef) || m_Defs.Contains(m_irondef2);
				return false;
			}
		}

		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_cobalt
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_cobaltdef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_nickel
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_nickeldef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_magnesium
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_magnesiumdef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_silicon
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_silicondef1);
				return false;
			}
		}

		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_silver
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_silverdef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_gold
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_golddef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_platinum
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_platinumdef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_uranium
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_uraniumdef1);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public bool has_ice
		{
			get
			{
				if (m_cache != null)
					return m_Defs.Contains(m_icedef1) || m_Defs.Contains(m_icedef2);
				return false;
			}
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public Vector3D center
		{
			get
			{
				return m_position;
			}

		}
	}
}
