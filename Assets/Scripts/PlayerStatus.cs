using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
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
	public float m_speed = 0.0f;


	// ----------------------------------------------
	// アクセサ

	public int   Hp               { get { return m_hp;               } }

	public int   Atk              { get { return m_atk;              } }

	public float Energy           { get { return m_energy;           } }

	public float HealEnergyPerSec { get { return m_healEnergyPerSec; } }

	public float Speed            { get { return m_speed;            } }
}
