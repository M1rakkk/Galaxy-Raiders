using UnityEngine;

public sealed class DamageSource : MonoBehaviour
{
    [Min(1)]
    public int damage = 1;
}

