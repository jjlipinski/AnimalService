DROP TABLE Habitats;
DROP TABLE Animals;

CREATE TABLE IF NOT EXISTS Habitats(Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    Habitat TEXT);

CREATE TABLE IF NOT EXISTS Animals(
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    Species TEXT, 
    HabitatId INTEGER,
    FOREIGN KEY (HabitatId) REFERENCES Habitats (Id));

INSERT INTO Habitats(Habitat)
    VALUES('Savannah'),
    ('Arctic'),
    ('Jungle');

INSERT INTO Animals(Species, HabitatId)
    VALUES('Giraffe',(SELECT Id FROM Habitats WHERE (Habitat = 'Savannah'))),
    ('Walrus',(SELECT Id FROM Habitats WHERE (Habitat = 'Arctic'))),
    ('Tiger',(SELECT Id FROM Habitats WHERE (Habitat = 'Jungle')));

-- practice deleting an animal
--DELETE FROM Animals WHERE Animals.Id = 1;

-- create select statement to return a list of animal id, animal species and habitat name
SELECT Animals.Id, Animals.Species, Habitats.Habitat
FROM Animals
INNER JOIN Habitats ON Animals.HabitatId = Habitats.Id;


DELETE FROM Animals WHERE Animals.Id = 9;


SELECT Animals.Id, Animals.Species, Habitats.Habitat
FROM Animals
INNER JOIN Habitats ON Animals.HabitatId = Habitats.Id;

INSERT INTO Habitats(Habitat) VALUES('House');

INSERT INTO Animals(Species, HabitatId) 
    VALUES('dog',(SELECT Id FROM Habitats WHERE (Habitat = 'House')));

    SELECT Animals.Id, Animals.Species, Habitats.Habitat
FROM Animals
INNER JOIN Habitats ON Animals.HabitatId = Habitats.Id;