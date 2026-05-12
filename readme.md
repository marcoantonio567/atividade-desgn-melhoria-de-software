# Reverse Engineering and Refactoring Activity

## Objective
To reverse engineer the legacy code available in the activity repository, identifying responsibilities, implicit business rules, parameter meanings, redocumentation opportunities, and restructuring proposals.

## Part 01 - Reverse Engineering
The analyzed class was `PedidoProcessor`, responsible for processing orders. During the analysis, it was identified that the class not only processes orders but also performs:

- input validations;

- subtotal calculation;

- application of discounts;

- calculation of national and international freight;

- calculation of interest for installments;

- generation of alerts;

- sending emails;

- persistence of logs.

## Part 02 - Redocumentation
The following was performed:

- improvement of variable, method, and class names;

- insertion of explanatory comments in the original code;

- creation of a class diagram representing the existing domain.

## Part 3 - Restructuring
The separation of responsibilities into specific components was proposed, reducing coupling and increasing the logical clarity of the system. The main improvement suggested was the creation of specialized services for validation, calculation, notification, and logging.

## Conclusion
The analysis showed that the legacy system has low cohesion and an excess of centralized responsibilities. The proposed refactoring improves readability, maintainability, testability, and future evolution of the software.
