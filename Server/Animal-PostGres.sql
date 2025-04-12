DROP TABLE IF EXISTS Animals;
DROP TABLE IF EXISTS Habitats;

CREATE TABLE IF NOT EXISTS Habitats (
    Id SERIAL PRIMARY KEY,
    Habitat TEXT
);

CREATE TABLE IF NOT EXISTS Animals (
    Id SERIAL PRIMARY KEY,
    Species TEXT,
    HabitatId INTEGER,
    FOREIGN KEY (HabitatId) REFERENCES Habitats (Id)
);

INSERT INTO Habitats(Habitat)
VALUES
    ('Savannah'),
    ('Arctic'),
    ('Jungle');

INSERT INTO Animals(Species, HabitatId)
VALUES
    ('Giraffe', (SELECT Id FROM Habitats WHERE Habitat = 'Savannah')),
    ('Walrus', (SELECT Id FROM Habitats WHERE Habitat = 'Arctic')),
    ('Tiger', (SELECT Id FROM Habitats WHERE Habitat = 'Jungle'));
