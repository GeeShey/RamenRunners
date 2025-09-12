using TMPro;
using UnityEngine;
using System.Numerics;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;
    public string TOTALCURRENCY = "0";
    public TextMeshPro currencyCounter;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
        updateCounter();
    }

    public void updateCounter()
    {
        currencyCounter.text = FormatCurrency(TOTALCURRENCY);
    }

    public void addFunds(int amount)
    {
        BigInteger currentCurrency = BigInteger.Parse(TOTALCURRENCY);
        BigInteger amountToAdd = new BigInteger(amount);
        BigInteger newTotal = currentCurrency + amountToAdd;

        TOTALCURRENCY = newTotal.ToString();

        updateCounter();
    }

    public void addFunds(string amount)
    {
        // Convert strings to BigInteger for arithmetic
        BigInteger currentCurrency = BigInteger.Parse(TOTALCURRENCY);
        BigInteger amountToAdd = BigInteger.Parse(amount);

        // Perform addition
        BigInteger newTotal = currentCurrency + amountToAdd;

        // Convert back to string
        TOTALCURRENCY = newTotal.ToString();

        // Update the display
        updateCounter();
    }

    // Optional: Format currency with commas for better readability
    private string FormatCurrency(string currencyString)
    {
        // Parse to BigInteger and back to ensure valid number
        BigInteger currency = BigInteger.Parse(currencyString);

        string numberStr = currency.ToString();

        // Add commas every 3 digits from right
        if (numberStr.Length > 3)
        {
            string result = "";
            int digitCount = 0;

            for (int i = numberStr.Length - 1; i >= 0; i--)
            {
                if (digitCount > 0 && digitCount % 3 == 0)
                {
                    result = "," + result;
                }
                result = numberStr[i] + result;
                digitCount++;
            }

            return result;
        }

        return numberStr;
    }

    public BigInteger GetCurrencyAsBigInteger()
    {
        return BigInteger.Parse(TOTALCURRENCY);
    }

    public void setCurrency(string newAmount)
    {
        // Validate that the string is a valid number
        if (BigInteger.TryParse(newAmount, out BigInteger _))
        {
            TOTALCURRENCY = newAmount;
            updateCounter();
        }
        else
        {
            Debug.LogError("Invalid currency amount: " + newAmount);
        }
    }
}