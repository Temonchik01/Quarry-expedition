using NUnit.Framework;
using UnityEngine;
using BtlSys;
using System.Collections.Generic;

public class BattleLogicTests
{
    private GameObject _battleManagerObject;
    private BattleManager _battleManager;

    [SetUp]
    public void SetUp()
    {
        _battleManagerObject = new GameObject("BattleManager");
        _battleManager = _battleManagerObject.AddComponent<BattleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_battleManagerObject);
    }

    [Test]
    public void Test_BattleAction_SpeedCalculation()
    {
        // Arrange
        var userObj = new GameObject();
        var user = userObj.AddComponent<Combatant>();
        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.speed = 10;
        user.data = data;
        user.Initialize();

        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.hasPriority = true;

        // Act
        var action = new BattleAction(user, new List<Combatant>(), attack);

        // Assert: 10 (base) + 1000 (priority) = 1010
        Assert.AreEqual(1010, action.speed, "Action speed with priority should be base + 1000");

        Object.DestroyImmediate(userObj);
        Object.DestroyImmediate(data);
        Object.DestroyImmediate(attack);
    }
}
