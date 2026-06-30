Feature: Power Position Reporting
    As a power trading operations team
    I want the day-ahead aggregated trade positions written to a CSV report
    So that I can review tomorrow's hourly volumes

Scenario: Generating a day-ahead CSV report from real trade data
    Given the power trade service is available
    When the day-ahead aggregate trade positions are requested
    Then the result should contain 24 hourly positions
    And a CSV report should be generated with 24 hourly rows

Scenario: Report file is named after the day-ahead date
    Given the power trade service is available
    When the day-ahead aggregate trade positions are requested
    Then the generated CSV file name should start with "PowerPosition_"

Scenario: Retrying past transient upstream failures
    Given the power trade service is available
    When the day-ahead aggregate trade positions are requested 5 times in a row
    Then every request should return a non-empty result

Scenario: Full reporter loop produces a single report on first run
    Given the power position report service is available
    When the reporter runs and is cancelled shortly after the first iteration
    Then exactly one report file should exist