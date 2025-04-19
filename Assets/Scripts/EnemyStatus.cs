using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
	// ----------------------------------------------
	// 設定項目

	[SerializeField]
	public int m_hp = 0;

	[SerializeField]
	public int m_atk = 0;

	[SerializeField]
	public float m_energy = 100;

	[SerializeField]
	public float m_healEnergyPerSec = 1;

	[SerializeField]
	public int m_point = 10;

	[SerializeField]
	public float m_speed = 0.0f;

	[SerializeField]
	public float m_searchRange = 0.0f;


	// ----------------------------------------------
	// アクセサ

	public int   Hp               { get { return m_hp;               } }

	public int   Atk              { get { return m_atk;              } }

	public float Energy           { get { return m_energy;           } }

	public float HealEnergyPerSec { get { return m_healEnergyPerSec; } }

	public int   Point            { get { return m_point;            } }

	public float Speed            { get { return m_speed;            } }

	public float SearchRange      { get { return m_searchRange;      } }
}
