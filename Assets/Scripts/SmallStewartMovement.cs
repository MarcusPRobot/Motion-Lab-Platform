using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class SmallStewartMovement : MonoBehaviour
{

    [SerializeField] ArticulationBody[] Cylinders = new ArticulationBody[6];
    [SerializeField] bool sineWave = false;
    [SerializeField] float speed = 1.0f;
    

    void Start()
    {
        
    }


    void Update()
    {
        
        if (sineWave)
        {
            runSineWave();
        }
    }

    void UpdateCylinder(ArticulationBody[] cylinders, float value)
    {

        // Update all cylinders with given value
        for(int i = 0; i < cylinders.Length; i++)
        {
            ArticulationDrive xDrive = cylinders[i].xDrive;
            xDrive.target = value;
            cylinders[i].xDrive = xDrive;
        }
        
    }

    void runSineWave()
    {
        // Calculate the sine wave value based on time and speed
        float sineValue = Mathf.Sin(Time.time * speed) * 0.5f + 0.5f; // Normalized to [0, 1]
        
        // Update all cylinders with the sine wave value
        UpdateCylinder(Cylinders, sineValue);
    }
    
}
