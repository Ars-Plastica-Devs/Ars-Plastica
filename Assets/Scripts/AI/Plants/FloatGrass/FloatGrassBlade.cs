using UnityEngine;

public abstract class FloatGrassBlade : PlantBase
{
    protected NoduleProducer NoduleProducer;

    public float DaysToGrown = 5f;
    public float DaysOld;
    public float FloatSpeed;
    public float FloatRange;

    protected IGrower Grower;

    //public GameObject[] GrowthStages;
    //Syncvar might not work with inheritance
    //[SyncVar(hook = "OnGrowthIndexChange")]
    //public int CurrentGrowthStageIndex;

    public abstract void BladeUpdate(float dt);
    public abstract void BladeFixedUpdate();

    protected virtual void GrowingStart()
    {
        //CurrentGrowthStageIndex = 0;
        Grower.StartGrowing();
    }

    protected virtual void GrowingUpdate()
    {
        Grower.GrowthUpdate(DaysOld / DaysToGrown);
        /*if (GrowthStages.Length == 0)
            return;

        var i = Mathf.Clamp((int)((DaysOld / DaysToGrown) * (GrowthStages.Length - 1)), 0, GrowthStages.Length - 1);

        //We want this check to prevent the SyncVar hook 
        //from being called unless there is a change
        if (i != CurrentGrowthStageIndex)
            CurrentGrowthStageIndex = i;*/
    }

    protected virtual void GrownStart()
    {

    }

    protected virtual void GrownUpdate()
    {

    }

    protected virtual void Die()
    {
        BirthTime = Time.time;
    }

    /*private void OnGrowthIndexChange(int newVal)
    {
        if (CurrentGrowthStageIndex == newVal)
            return;

        CurrentGrowthStageIndex = newVal;
        if (CurrentGrowthStageIndex >= 0 && CurrentGrowthStageIndex < GrowthStages.Length)
        {
            //Make sure the correct growth stage is the only one active
            for (var i = 0; i < GrowthStages.Length; i++)
            {
                GrowthStages[i].SetActive(i == CurrentGrowthStageIndex);
            }
        }
    }*/
}
