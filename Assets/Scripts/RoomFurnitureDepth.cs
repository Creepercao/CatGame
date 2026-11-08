using UnityEngine;

namespace CatGame
{
    /// <summary>All pieces of a tall prop share its ground depth, including on resize.</summary>
    public sealed class RoomFurnitureDepth : MonoBehaviour
    {
        public Vector3 LocalGround;
        public SpriteRenderer[] Pieces;
        void LateUpdate()
        {
            int order = Mathf.RoundToInt((10f - transform.TransformPoint(LocalGround).y) * 100f);
            foreach (var piece in Pieces) if (piece != null) piece.sortingOrder = order;
        }
    }
}
