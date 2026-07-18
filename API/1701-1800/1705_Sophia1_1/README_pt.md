# Estratégia Sophia 1_1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sophia 1_1 é uma estratégia de negociação em grade baseada no princípio de martingale.
A estratégia abre uma posição após quatro velas consecutivas se moverem na mesma direção:
- Quatro velas de alta ativam uma entrada vendida.
- Quatro velas de baixa ativam uma entrada comprada.

Uma vez no mercado, o algoritmo adiciona posições toda vez que o preço se move contra a posição atual em um número fixo de passos de preço (`Pip Step`).
O volume de cada operação adicional é multiplicado por `Lot Exponent`, formando uma grade de martingale clássica.

O gerenciamento de risco é tratado por meio de `Take Profit`, `Stop Loss` e um trailing stop opcional.
O mecanismo de trailing começa após o lucro atingir `Trail Start` e acompanha o nível de stop em `Trail Stop` passos de preço.

## Parâmetros
- **Volume** – volume base para a primeira operação.
- **Pip Step** – distância em passos de preço antes de adicionar uma nova posição.
- **Lot Exponent** – multiplicador para o volume de cada operação adicional.
- **Max Trades** – número máximo de posições na grade.
- **Take Profit** – alvo de lucro em passos de preço a partir do preço médio de entrada.
- **Stop Loss** – limite de perda em passos de preço a partir do preço médio de entrada.
- **Use Trailing** – habilitar ou desabilitar o trailing stop.
- **Trail Start** – lucro necessário antes de o trailing stop ficar ativo.
- **Trail Stop** – distância do trailing stop em passos de preço.
- **Candle Type** – período das velas usadas para cálculos.
