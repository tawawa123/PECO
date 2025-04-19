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
	public int m_atkDamage = 0;

	[SerializeField]
	public int m_atkPower = 0;

	[SerializeField]
	public float m_speed = 0.0f;

	[SerializeField]
	public float m_viewRange = 0.0f;

	[SerializeField]
	public float m_warningRange = 0.0f;

	[SerializeField]
	public float m_viewAngle = 0.0f;

	[SerializeField]
	public float m_stun = 0;


	// ----------------------------------------------
	// アクセサ

	public int GetHp 				{ get{ return m_hp; } }
	public int GetAtkDamage 		{ get{ return m_atkDamage; } }
	public int GetAtkPower 			{ get{ return m_atkPower; } }
	public float GetSpeed 			{ get{ return m_speed; } }
	public float GetViewRange 		{ get{ return m_viewRange; } }
	public float GetWarningRange 	{ get{ return m_warningRange; } }
	public float GetViewAngle 		{ get{ return m_viewAngle; } }
	public float GetStun 			{ get{ return m_stun; } }
}
