﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEffect : ActionEffect
{
    public FieldObject objPrefab;
    public override IEnumerator ApplyEffect(Combatant user, Combatant target, ExtraData data)
    {
        Debug.Log(user.DisplayName + " can't spawn " + objPrefab.DisplayName + " in an occupied square!");
        yield break;
    }

    public override IEnumerator ApplyEffect(Combatant user, Pos target, ExtraData data)
    {
        var obj = Instantiate(objPrefab.gameObject, BattleGrid.main.GetSpace(target), Quaternion.identity);
        var fObj = obj.GetComponent<FieldObject>();
        BattleGrid.main.SetObject(target, fObj);
        yield return new WaitForSeconds(effectWaitTime);
    }
}
