using System;
using System.Text;
using System.ComponentModel;
using System.Collections;

using SEModAPIInternal.API.Entity.Sector.SectorObject;



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
				return emp.asteroid.Name;
			}
		}

		public override string Description
		{
			get
			{
				SeDropshipAsteroids roid = this.collection[index];
				StringBuilder sb = new StringBuilder();
				sb.Append(roid.asteroid.Name);
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
				return asteroids.asteroid.Name;
			}
			return base.ConvertTo(context,culture,value,destType);
		}
	}

	[Serializable()]
	[TypeConverter(typeof(AsteroidsConverter))]
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
			m_sizex = (int)m_asteroid.Size.X;
			m_sizey = (int)m_asteroid.Size.Y;
			m_sizez = (int)m_asteroid.Size.Z;
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
}
