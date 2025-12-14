[System.Serializable]
public class PlayerStatus
{
	// ----------------------------------------------
	// 設定項目
	public int m_hp = 0;
	public int m_atkDamage = 0;
	public int m_atkPower = 0;
	public float m_stumina = 0.0f;
	public float m_walkSpeed = 0.0f;
	public float m_runSpeed = 0.0f;
	public float m_rotationRate = 0.0f;
	public float m_avoidPower = 0.0f;
	public bool m_stun = false;


	// ----------------------------------------------
	// アクセサ

	public int GetHp 					{ get { return m_hp; } }
	public int GetAtkDamage 			{ get { return m_atkDamage; } }
	public int GetAtkPower				{ get { return m_atkPower; } }
	public float GetStumina				{ get { return m_stumina; } }
	public float GetWalkSpeed			{ get { return m_walkSpeed; } }
	public float GetRunSpeed 			{ get { return m_runSpeed; } }
	public float GetRotationRate 		{ get { return m_rotationRate; } }
	public float GetAvoidPower			{ get { return m_avoidPower; } }
	public bool GetStun 				{ get { return m_stun; } }
}
