# Estratégia MTC Combo v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do script MetaTrader "MTC Combo v2 (barabashkakvn's edition)".

## Lógica
- Usa a inclinação de uma média móvel para determinar a tendência básica.
- Filtro de perceptron opcional calcula a soma ponderada das diferenças recentes de preço de abertura ao longo de defasagens configuráveis.
- O parâmetro `Pass` seleciona quais ramos do perceptron são usados:
  - 4: requer perceptron3 > 0 e perceptron2 > 0 para comprado; perceptron3 <= 0 e perceptron1 < 0 para vendido.
  - 3: usa perceptron2 > 0 para comprado.
  - 2: usa perceptron1 < 0 para vendido.
  - outros valores: opera apenas com base na inclinação da MA.

Os níveis de stop loss e take profit são obtidos dos parâmetros `Sl*` e `Tp*`.

## Parâmetros
- `MaPeriod` – comprimento da média móvel.
- `P2`, `P3`, `P4` – defasagens para os perceptrons.
- `Pass` – modo de decisão.
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3` – stop e alvo para cada ramo.
- `CandleType` – séries de candles a processar.

## Notas
A estratégia mantém uma única posição por vez e a fecha quando o stop loss ou take profit é atingido.

## Aviso
Apenas para uso educacional. Não constitui conselho de investimento.
