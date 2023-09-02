import numpy as np
import matplotlib.pyplot as plt


def display_results(outcomes):
    # Data
    categories = ['Results']
    wins, draws, losses = 0, 0, 0

    for i, outcome in enumerate(outcomes):
        if outcome == 0:
            draws += 1
        elif outcome == 1:
            if i % 2 == 0:
                wins += 1
            else:
                losses += 1
        else:
            if i % 2 == 1:
                wins += 1
            else:
                losses += 1

    total_matches = wins + draws + losses

    # Calculate percentages
    win_percentage = round((wins / total_matches) * 100, 2)
    draw_percentage = round((draws / total_matches) * 100, 2)
    loss_percentage = round((losses / total_matches) * 100, 2)

    # Create a figure and axis
    fig, ax = plt.subplots(figsize=(10, 1))  # Adjust the height as needed

    # Plot horizontal stacked bar chart
    bars_wins = ax.barh(categories, wins, color='green', label='Wins')
    bars_draws = ax.barh(categories, draws, left=wins, color='grey', label='Draws')
    bars_losses = ax.barh(categories, losses, left=wins + draws, color='red', label='Losses')

    # Display the actual results on top of each section
    for bar, value in zip(bars_wins, [win_percentage]):
        ax.text(bar.get_x() + bar.get_width() / 2, bar.get_y() + bar.get_height() / 2, f'{value}%', ha='center', va='center', color='black', fontweight='bold')

    for bar, value in zip(bars_draws, [draw_percentage]):
        ax.text(bar.get_x() + bar.get_width() / 2, bar.get_y() + bar.get_height() / 2, f'{value}%', ha='center', va='center', color='black', fontweight='bold')

    for bar, value in zip(bars_losses, [loss_percentage]):
        ax.text(bar.get_x() + bar.get_width() / 2, bar.get_y() + bar.get_height() / 2, f'{value}%', ha='center', va='center', color='black', fontweight='bold')

    # Display percentages inside the line
    # line_text = f'Wins: {win_percentage:.2f}% | Draws: {draw_percentage:.2f}% | Losses: {loss_percentage:.2f}%'
    line_text = f'Wins: {wins}    Draws: {draws}    Losses: {losses}'
    ax.text(0, 0.6, line_text, fontsize=12, va='center', fontweight='bold')

    # Customize the appearance
    ax.set_xlim(0, total_matches)
    ax.set_xticks([])  # Hide x-axis ticks
    ax.set_yticks([])  # Hide y-axis ticks

    # Adjust y-limits for title
    plt.subplots_adjust(top=0.7)

    # Display the chart
    # plt.show()
    plt.savefig("results.png")


def display_score_graph(outcomes):
    # Initialize cumulative scores
    player1_score = 0
    player2_score = 0
    player1_scores = []
    player2_scores = []

    # Calculate scores based on outcomes
    for i, outcome in enumerate(outcomes):
        if outcome == 0:
            player1_score += 1
            player2_score += 1
        elif outcome == 1:
            if i % 2 == 0:
                player1_score += 2
            else:
                player2_score += 2
        else:
            if i % 2 == 1:
                player1_score += 2
            else:
                player2_score += 2
        player1_scores.append(player1_score)
        player2_scores.append(player2_score)

    # Create a figure and axis
    plt.figure(figsize=(10, 6))
    plt.plot(player1_scores, label='Player 1')
    plt.plot(player2_scores, label='Player 2')

    # Adding labels and title
    plt.xlabel('Games')
    plt.ylabel('Score')
    plt.title('Player Scores')
    plt.legend()

    # Display the plot
    plt.grid(True)
    # plt.show()
    plt.savefig("scores.png")


def get_results(file_path):

    with open(file_path, 'r') as file:
        content = file.readlines()
    
    players = content[2]

    for line in content:
        if line.startswith("Results =>"):
            outcomes = list(map(int, line[len("Results =>"):].split()))
    
    return players, outcomes


players, outcomes = get_results('results.txt')

display_results(outcomes=outcomes)

display_score_graph(outcomes=outcomes)

# plt.show()

