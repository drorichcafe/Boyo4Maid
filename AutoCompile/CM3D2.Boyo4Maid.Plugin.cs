using System;
using System.Collections.Generic;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Boyo4Maid
{
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Boyo4Maid"), PluginVersion("0.0.0.0")]
	public class Boyo4Maid : PluginBase
	{
		public class Config
		{
			public class Joint
			{
				public string Name = string.Empty;
				public Vector3 Offset = Vector3.zero;
				public float Spring = 0.0f;
				public float MaxDistance = 0.0f;
				public float Damper = 0.01f;
			}

			public KeyCode KeyApply = KeyCode.Space;
			public List<Joint> Joints = new List<Joint>();
		}

		private static Config m_config = new Config();
		private static DateTime m_lastUpdate = new DateTime();

		public void Awake()
		{
			UnityEngine.Object.DontDestroyOnLoad(this);
		}

		public void OnLevelWasLoaded(int lv)
		{
			m_config = loadXml<Config>(System.IO.Path.Combine(this.DataPath, "Boyo4Maid.xml"));
		}

		public void Update()
		{
			var path = System.IO.Path.Combine(this.DataPath, "Boyo4Maid.xml");
			var time = System.IO.File.GetLastWriteTime(path);
			if (time != m_lastUpdate || Input.GetKeyDown(m_config.KeyApply))
			{
				m_config = loadXml<Config>(path);
				m_lastUpdate = time;

				foreach (var cmp in FindObjectsOfType<Boyo4>())
				{
					GameObject.Destroy(cmp);
				}

				var gos = FindObjectsOfType<GameObject>();
				foreach (var jnt in m_config.Joints)
				{
					foreach (var go in gos)
					{
						if (go.name == jnt.Name)
						{
							var parent = go.transform.parent;
							var cmp = go.AddComponent<Boyo4>();
							go.transform.position = parent.position;
							cmp.goal = parent;
							cmp.spring = jnt.Spring;
							cmp.maxdistance = jnt.MaxDistance;
							cmp.damper = jnt.Damper;
							cmp.offset = jnt.Offset;
						}
					}
				}
			}
		}

		private T loadXml<T>(string path)
		{
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
			using (var sr = new System.IO.StreamReader(path, new System.Text.UTF8Encoding(true)))
			{
				return (T)serializer.Deserialize(sr);
			}
		}
	}

	public class Boyo4 : MonoBehaviour
	{
		public Transform goal = null;
		public float spring = 1.0f;
		public float maxdistance = 0.0f;
		public float damper = 0.1f;
		public Vector3 vel = Vector3.zero;
		public Vector3 oldpos = Vector3.zero;
		public Vector3 offset = Vector3.zero;
		
		private void Awake()
		{
			oldpos = transform.position;
		}

		private void LateUpdate()
		{
			var goalPos = goal.position + goal.TransformVector(offset);
			var dt = Time.deltaTime;
			var v = goalPos - oldpos;
			vel += (v * spring) * dt;

			transform.position += vel * dt;
			v = transform.position - goalPos;
			if (v.magnitude > maxdistance)
			{
				transform.position = goalPos + v.normalized * maxdistance;
				vel += -v * spring * dt;
			}

			oldpos = transform.position;
			vel -= vel * dt *  damper;
		}
	}
}