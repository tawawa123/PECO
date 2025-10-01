using UnityEngine;
using UnityEngine.UI;

namespace App.BaseSystem.DataStores.ScriptableObjects.Item
{
    /// <summary>
    /// ステータスを持つオブジェクトのデータ群 (対象: プレイヤー、敵、破壊可能オブジェクトなど)
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObject/Data/Item")]
    public class ItemData : BaseData
    {
        public int Heal
        {
            get => heal;
            set => heal = value;
        }
        [SerializeField]
        private int heal;

        public int Buff_Atk
        {
            get => buff_atk;
            set => buff_atk = value;
        }
        [SerializeField]
        private int buff_atk;

        public int Buff_Dfn
        {
            get => buff_dfn;
            set => buff_dfn = value;
        }
        [SerializeField]
        private int buff_dfn;

        public int Price
        {
            get => price;
            set => price = value;
        }
        [SerializeField]
        private int price;

        public int Max_Have
        {
            get => max_have;
            set => max_have = value;
        }
        [SerializeField]
        private int max_have;

        public Sprite Image
        {
            get => image;
            set => image = value;
        }
        [SerializeField]
        private Sprite image;
    }
}
