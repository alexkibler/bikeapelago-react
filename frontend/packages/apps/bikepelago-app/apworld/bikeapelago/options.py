import typing
from dataclasses import dataclass

from Options import Choice, Range, OptionDict, PerGameCommonOptions


class CheckCount(Range):
    """The number of intersections/locations to generate for this world."""

    display_name = "Check Count"
    range_start = 10
    range_end = 1000
    default = 100


class GoalType(Choice):
    """The goal required to complete the game."""

    display_name = "Goal"
    option_all_intersections = 0
    option_percentage = 1
    default = 0


class StartingNodes(Range):
    """The number of Node Unlock items placed into the player's starting inventory.
    These nodes are immediately available at the start of the game."""

    display_name = "Starting Nodes"
    range_start = 1
    range_end = 50
    default = 3


class ProgressionMode(Choice):
    """How the player's progression is gated."""
    display_name = "Progression Mode"
    option_quadrant = 0
    option_radius = 1
    option_free = 2
    default = 0


@dataclass
class BikeapelagoOptions(PerGameCommonOptions):
    check_count: CheckCount
    goal_type: GoalType
    starting_nodes: StartingNodes
    progression_mode: ProgressionMode


bikeapelago_options: typing.Dict[str, type] = {
    "check_count": CheckCount,
    "goal_type": GoalType,
    "starting_nodes": StartingNodes,
    "progression_mode": ProgressionMode,
}
