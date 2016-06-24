﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GeneticAlgorithm : MonoBehaviour {

    // Constant values
    private const int NUMBER_INPUTS = 33;
    private const int NUMBER_OUTPUTS = 12;

    private const int POPULATION_SIZE = 40;


    // Other scripts
    TurnManager turnManager;

    class Unit {
        public double[,] chromosome;
        public bool tested = false;
        public float fitness = 0;

        public Unit() {}

        public Unit(double[,] chromosomeMatrix) {
            chromosome = chromosomeMatrix;
        }

        public Unit(double[,] chromosomeMatrix, bool testedValue, float fitnessValue) {
            chromosome = chromosomeMatrix;
            tested = testedValue;
            fitness = fitnessValue;
        }
    }

    List<Unit> gamePopulation = new List<Unit>();
    int generation;

    // Auxiliar variables
    int i, j;
    int actualIndex = 0;

    void Start() {
        // Loading the population from disk
        bool gamePopupationLoaded = LoadPopulation(gamePopulation, "population.data");

        // If failed, create
        if (!gamePopupationLoaded)
            if (CreatePopulation(gamePopulation)) {
                generation = 0;
                SavePopulation(gamePopulation, "population.data");
            }

        turnManager = GameObject.Find("Main Camera").GetComponent<TurnManager>();

        // Begin the game with this
        Invoke("Evaluation", 0.3f);
    }

    private bool LoadPopulation(List<Unit> population, System.String fileName) {
        try {
            System.String fileDirectory = @"Assets\SavedData\" + fileName;
            if (File.Exists(fileDirectory)) { // Checking if the file exists

                // Opening the file
                using (var filestream = File.Open(fileDirectory, FileMode.Open))
                using (var binaryStream = new BinaryReader(filestream)) {
                    bool testedValue;
                    float fitnessValue;
                    // Loading the generation information
                    generation = binaryStream.ReadInt32();
                    // Looking for all units
                    for (int unit = 0; unit < POPULATION_SIZE; unit++) {
                        double[,] chromosomeToLoad = new double[NUMBER_INPUTS, NUMBER_OUTPUTS];
                        // Taking the chromosome matrix
                        for (i = 0; i < NUMBER_INPUTS; i++)
                            for (j = 0; j < NUMBER_OUTPUTS; j++)
                                chromosomeToLoad[i, j] = binaryStream.ReadDouble(); // Probably there is a more efficient way to load the matrix
                        testedValue = binaryStream.ReadBoolean();
                        fitnessValue = binaryStream.ReadSingle();

                        Unit loadedUnit = new Unit(chromosomeToLoad, testedValue, fitnessValue);
                        population.Add(loadedUnit);
                    }
                }
                return true; // Success

            }
            else { // File don't exist
                print("File don't exist. Must create.");
                return false;
            }
        }
        catch(System.Exception e) {
            print("Exception when reading population. Error: " + e.Message);
            return false;
        }
    }

    private bool SavePopulation(List<Unit> population, System.String fileName) {
        try {
            System.String fileDirectory = @"Assets\SavedData\" + fileName;
            using (var filestream = File.Create(fileDirectory))
            using (var binarystream = new BinaryWriter(filestream)) {
                // Saving the generation information
                binarystream.Write(generation);
                // Saving all units
                for (int unit = 0; unit < POPULATION_SIZE; unit++) {
                    // Saving the chromosome matrix
                    for (i = 0; i < NUMBER_INPUTS; i++)
                        for (j = 0; j < NUMBER_OUTPUTS; j++)
                            binarystream.Write(population[unit].chromosome[i, j]);
                    binarystream.Write(population[unit].tested);
                    binarystream.Write(population[unit].fitness);
                }
            }
            return true; // Success
        }
        catch (System.Exception e) {
            print("WARNING: Failed to save the population data on disk. Error: " + e.Message);
            return false;
        }
    }

    private bool CreatePopulation(List<Unit> population) {
        try {
            // Certify that the population is empty
            population.Clear();

            for (int unit = 0; unit < POPULATION_SIZE; unit++) {
                // Instantiating the chomossomes
                double[,] chromosome = new double[NUMBER_INPUTS, NUMBER_OUTPUTS];

                // Creating the chromosomes 
                for (i = 0; i < NUMBER_INPUTS; i++)
                    for (j = 0; j < NUMBER_OUTPUTS; j++) {
                        // Chromosome for the left team
                        chromosome[i, j] = Random.Range(-1.0f, 1.0f);
                    }

                // Creating the 'complete' unit
                Unit newUnit = new Unit(chromosome);

                // Add them to their population
                population.Add(newUnit);

            }
            return true; // Success
        }
        catch (System.Exception e) {
            print("Failed to create the population. Error: " + e.Message);
            return false;
        }
        
    }

    private void Evaluation() {
        // Reset game values
        turnManager.ResetGame();

        // Setting the NN weight
        for (; actualIndex < POPULATION_SIZE / 2; actualIndex++) { // Looking for the first to be evaluated
            if (gamePopulation[2 * actualIndex].tested) {
                print(System.String.Format("Chromosomes already tested. Fitness: [{0}] {1:0.00}; [{2}] {3:0.00}", 2 * actualIndex, gamePopulation[2 * actualIndex].fitness, 2 * actualIndex + 1, gamePopulation[2 * actualIndex + 1].fitness));
                continue;
            }

            print(System.String.Concat("Left team chromosome: ", 2 * actualIndex, ". Right team chromosome: ", 2 * actualIndex + 1, "."));
            // If runs here, found the chromosomes to be used this time
            turnManager.SetNeuralNetworkWeights(gamePopulation[2 * actualIndex].chromosome, gamePopulation[2 * actualIndex + 1].chromosome);
            /*print("First chromosome");
            for (int i = 0; i < NUMBER_INPUTS; i++)
                for (int j = 0; j < NUMBER_OUTPUTS; j++)
                    print(System.String.Format("[{0}; {1}] = {2:0.00}", i, j, gamePopulation[2 * actualIndex].chromosome[i, j]));
            print("Second chromosome");
            for (int i = 0; i < NUMBER_INPUTS; i++)
                for (int j = 0; j < NUMBER_OUTPUTS; j++)
                    print(System.String.Format("[{0}; {1}] = {2:0.00}", i, j, gamePopulation[2 * actualIndex + 1].chromosome[i, j]));*/
            break;
        }

        // Check if finished evaluating the population
        if (actualIndex == POPULATION_SIZE / 2) {
            PrepareNextGeneration();
            return;
        }

        // If is here, there are still chromosomes to be evaluated, and it is already setted on the NN
        turnManager.BeginGame();

    }

    public void Evaluated(float leftTeamFitness, float rightTeamFitness) {
        // Setting the fitness
        gamePopulation[2 * actualIndex].fitness = leftTeamFitness;
        gamePopulation[2 * actualIndex + 1].fitness = rightTeamFitness;

        // Marking as evaluated
        gamePopulation[2 * actualIndex].tested = gamePopulation[2 * actualIndex + 1].tested = true;

        // Saving the population
        SavePopulation(gamePopulation, "population.data");

        // nextIndex
        actualIndex++;

        // Beggining the next battle
        Invoke("Evaluation", 3.0f);
    }

    private void PrepareNextGeneration() {
        print("Preparing next generation");
        generation++;
        List<Unit> nextPopulation = new List<Unit>();
        float crossoverRate = 0.9f;
        // Generating the next generation (selection + crossover)
        nextPopulation = Selection(nextPopulation, crossoverRate);
    }

    private Unit Roulette(List<Unit> population) {
        float fitnessSum = 0;
        // SUM of all fitness
        for (int i = 0; i < population.Count; i++)
            fitnessSum += population[i].fitness;
        // Random for selection
        float rouletteValue = Random.Range(0f, fitnessSum);
        // Looking for the selected chromosome
        Unit selectedUnit = null;
        for (int i = 0; i < population.Count; i++) {
            rouletteValue -= population[i].fitness;
            if (rouletteValue <= 0.00001f) {
                // Found the selected chromosome, transfer it to the new generation
                selectedUnit = population[i];
                population.RemoveAt(i);
                break;
            }
        }
        return selectedUnit;
    }

    private List<Unit> Selection(List<Unit> nextPopulation, float crossoverRate) {
        // 1 - Letting all fitness values become a least 0
        /*float smallestFitness = 1000, biggestFitness = -1000;
        int biggestFitnessIndex = 0, secondBiggestFitnessIndex = 0;
        for (int i = 0; i < POPULATION_SIZE; i++) {
            // Finding the smallest fitness
            if (gamePopulation[i].fitness < smallestFitness)
                smallestFitness = gamePopulation[i].fitness;
            // Finding the biggest fitness
            if (gamePopulation[i].fitness > biggestFitness) {
                biggestFitness = gamePopulation[i].fitness;
                secondBiggestFitnessIndex = biggestFitnessIndex;
                biggestFitnessIndex = i;
            }
        }*/

        gamePopulation.Sort((s1, s2) => s2.fitness.CompareTo(s1.fitness));
        // Adding the smallest fitness on everyone
        /*if (smallestFitness < 0) {
            for (int i = 0; i < POPULATION_SIZE; i++)
                gamePopulation[i].fitness += smallestFitness;
        }*/
        if (gamePopulation[POPULATION_SIZE - 1].fitness < 0)
            for (int i = 0; i < POPULATION_SIZE; i++)
                gamePopulation[i].fitness -= gamePopulation[POPULATION_SIZE - 1].fitness;

        // 2 - Transfer the 2 best chromosome to the new population (elitism)
        nextPopulation.Add(gamePopulation[0]);
        nextPopulation.Add(gamePopulation[1]);
        //gamePopulation.RemoveAt(biggestFitnessIndex);

        // 3 - Select using the Roulette algorithm, and make the crossover
        Unit firstParent, secondParent;
        while (nextPopulation.Count < POPULATION_SIZE) {
            firstParent = Roulette(gamePopulation);
            secondParent = Roulette(gamePopulation);
            // Checking if will happen crossover
            if (Random.Range(0f, 1f) < crossoverRate) {
                Unit[] offspring = Crossover(firstParent.chromosome, secondParent.chromosome);
                nextPopulation.Add(offspring[0]);
                nextPopulation.Add(offspring[1]);
            }
            else { // Won't happen crossover
                nextPopulation.Add(firstParent);
                nextPopulation.Add(secondParent);
            }
        }

        // 4 - Reset the fitness value of the copied chromosomes
        /*for (int i = 0; i < nextPopulation.Count; i++)
            nextPopulation[i].tested = false;*/

        return nextPopulation;
    }

    private Unit[] Crossover(double[,] firstParent, double[,] secondParent) {
        Unit[] offspring = new Unit[2];
        double[,] firstOffspringChromosome = new double[NUMBER_INPUTS, NUMBER_OUTPUTS];
        double[,] secondOffspringChromosome = new double[NUMBER_INPUTS, NUMBER_OUTPUTS];
        int i, j;
        // The Crossover algorithm (Uniform crossover)
        float exchangeProbability = 0.5f;
        for (i = 0; i < NUMBER_INPUTS; i++ )
            for (j = 0; j < NUMBER_OUTPUTS; j++ ) {
                if (Random.Range(0f, 1f) < exchangeProbability) {
                    firstOffspringChromosome[i, j] = firstParent[i, j];
                    secondOffspringChromosome[i, j] = secondParent[i, j];
                }
                else {
                    firstOffspringChromosome[i, j] = secondParent[i, j];
                    secondOffspringChromosome[i, j] = firstParent[i, j];
                }
            }
        // Putting them on the 'Unit' variable
        offspring[0] = new Unit(firstOffspringChromosome);
        offspring[1] = new Unit(secondOffspringChromosome);
        return offspring;
    }

}
