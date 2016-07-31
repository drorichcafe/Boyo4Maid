using System;
using System.Collections.Generic;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Boyo4Maid
{
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Boyo4Maid"), PluginVersion("0.0.0.1")]
	public class Boyo4Maid : PluginBase
	{
		public class Config
		{
			public class Maid
			{
				public class Joint
				{
					public string Name = string.Empty;
					public Vector3 Offset = Vector3.zero;
					public float Spring = 0.0f;
					public float MaxDistance = 0.0f;
					public float Damper = 0.01f;
				}

				public string Name = string.Empty;
				public List<Joint> Joints = new List<Joint>();
			}

			public KeyCode KeyApply = KeyCode.Space;
			public List<Maid> Maids = new List<Maid>();
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

				for (int i = 0; i < GameMain.Instance.CharacterMgr.GetMaidCount() - 1; ++i)
				{
					var md = GameMain.Instance.CharacterMgr.GetMaid(i);
					if (md == null || md.gameObject == null) break;

					foreach (var maid_config in m_config.Maids)
					{
						if (maid_config.Name != md.Param.status.last_name + md.Param.status.first_name) continue;
						var gos = getMaidChildren(md.gameObject.transform);

						foreach (var jnt in maid_config.Joints)
						{
							if (!gos.ContainsKey(jnt.Name))
							{
								Console.WriteLine(jnt.Name + " not found");
								continue;
							}

							foreach (var go in gos[jnt.Name])
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
		}

		private T loadXml<T>(string path)
		{
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
			using (var sr = new System.IO.StreamReader(path, new System.Text.UTF8Encoding(true)))
			{
				return (T)serializer.Deserialize(sr);
			}
		}

		private List<Transform> getChildren(Transform parent)
		{
			List<Transform> ret = new List<Transform>();

			foreach (Transform child in parent)
			{
				ret.Add(child);
				ret.AddRange(getChildren(child));
			}

			return ret;
		}

		private Dictionary<string, List<GameObject>> getMaidChildren(Transform root)
		{
			var ret = new Dictionary<string, List<GameObject>>();

			foreach (var tr in getChildren(root))
			{
				var go = tr.gameObject;
				if (go != null)
				{
					if (!ret.ContainsKey(go.name))
					{
						ret.Add(go.name, new List<GameObject>());
					}

					ret[go.name].Add(go);
				}
			}

			return ret;
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