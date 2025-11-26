using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum PhilosopherName
{
    Socrates,
    Plato,
    Aristotle,
    Decartes,
    Kant
}

public static class PhilosopherExtensions
{
    public static string ToName(this PhilosopherName p) =>
        p switch
        {
            PhilosopherName.Socrates => "Socrates",
            PhilosopherName.Plato => "Plato",
            PhilosopherName.Aristotle => "Aristotle",
            PhilosopherName.Kant => "Kant",
            PhilosopherName.Decartes => "Decartes",
            _ => throw new ArgumentOutOfRangeException()
        };
}
