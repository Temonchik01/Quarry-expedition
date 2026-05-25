using NUnit.Framework;
using UnityEngine;
using BtlSys;

public class CombatantTests
{
    private GameObject _combatantObject;
    private Combatant _combatant;
    private CharacterData _mockData;

    [SetUp]
    public void SetUp()
    {
        _combatantObject = new GameObject();
        _combatant = _combatantObject.AddComponent<Combatant>();
        _mockData = ScriptableObject.CreateInstance<CharacterData>();
        
        _mockData.maxHealth = 100;
        _mockData.defense = 20;
        _mockData.attackPower = 30;
        _mockData.speed = 15;

        _combatant.data = _mockData;
        _combatant.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_combatantObject);
        Object.DestroyImmediate(_mockData);
    }

    [Test]
    public void Test_TakeDamage_CalculatesCorrectlyWithDefense()
    {
        // Arrange: Вхідна чиста шкода становить 50 одиниць. Захист сутності — 20.
        int rawDamage = 50;

        // Act: Виклик методу обробки отримання шкоди
        _combatant.TakeDamage(rawDamage);

        // Assert: Очікувана чиста шкода = 50 - 20 = 30. Поточне здоров'я: 100 - 30 = 70.
        Assert.AreEqual(70, _combatant.currentHP, "Розрахунок HP з урахуванням захисту виконано некоректно!");
    }

    [Test]
    public void Test_ApplyStatusEffect_ModifiesStatsCorrectly()
    {
        // Act: Накладання негативного ефекту стану Paralysis на 3 ходи
        _combatant.ApplyStatusEffect(StatusEffectType.Paralysis, 3);

        // Assert: Базова швидкість 15. Дебаф зменшує її на 40 одиниць (модифікатор швидкості).
        Assert.AreEqual(-25, _combatant.currentSpeed, "Дебаф швидкості від ефекту Paralysis розраховано неправильно!");
    }
}