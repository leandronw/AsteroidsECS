using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public class AsteroidsMaterialsReference : IComponentData
{
    public List<Material> Materials;
}
