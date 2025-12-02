using UnityEngine;
using Fusion;

public struct NetworkScoreData : INetworkStruct
{
    [Networked, Capacity(2)]
    public NetworkArray<int> Scores => default;

    [Networked]
    public int PlayerCount { get; set; }
}
