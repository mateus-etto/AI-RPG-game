﻿using UnityEngine;
using System.Collections;

public class NeuralNetwork : MonoBehaviour {

    private int numberVariables = 41;

    private double[] weightCommand;
    private double[] weightPriority;

    private double[] input;
    private double[] output;

	void Start () {
        weightCommand = new double[numberVariables];
        weightPriority = new double[numberVariables];

        input = new double[numberVariables];
        output = new double[2];
	}

	void Update () {
	
	}
}