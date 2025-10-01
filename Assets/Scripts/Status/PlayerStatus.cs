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
	public int m_atkDamage = 0;

	[SerializeField]
	public int m_atkPower = 0;

	[SerializeField]
	public float m_heal = 1;

	[SerializeField]
	public float m_walkSpeed = 0.0f;

	[SerializeField]
	public float m_runSpeed = 0.0f;

	[SerializeField]
	public float m_rotationRate = 0.0f;

	[SerializeField]
	public float m_avoidPower = 0.0f;

	[SerializeField]
	public float m_stun = 0;


	// ----------------------------------------------
	// アクセサ

	public int GetHp 					{ get { return m_hp; } }
	public int GetAtkDamage 			{ get { return m_atkDamage; } }
	public int GetAtkPower				{ get { return m_atkPower; } }
	public float GetHeal 				{ get { return m_heal; } }
	public float GetWalkSpeed			{ get { return m_walkSpeed; } }
	public float GetRunSpeed 			{ get { return m_runSpeed; } }
	public float GetRotationRate 		{ get { return m_rotationRate; } }
	public float GetAvoidPower			{ get { return m_avoidPower; } }
	public float GetStun 				{ get { return m_stun; } }
}
