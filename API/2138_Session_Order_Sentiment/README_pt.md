# Estratégia de Sentimento de Ordens por Sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia opera com base no desequilíbrio entre ordens de compra e venda observado no livro de ordens. Ela mede as proporções de contagens de ordens e volumes totais para ambos os lados do livro e abre uma posição quando a dominância de um lado supera os limiares configuráveis. O trading é permitido apenas durante uma janela de tempo especificada.

Após abrir uma posição, os limiares são reduzidos para monitorar o lado oposto. Se o lado oposto crescer além desses limiares reduzidos, a posição é fechada. Um stop loss e um take profit também são aplicados usando pontos de preço absolutos.

## Regras de trading
- **Entrada comprada**: Comprar quando
  - `BUY volume / SELL volume >= DiffVolumesEx` e `BUY orders / SELL orders >= DiffTradersEx`
  - Qualquer lado atende `MinTraders` e `MinVolume`
  - O tempo atual passa `CheckTradingTime`
- **Entrada vendida**: Vender quando a lógica acima é espelhada para o lado vendedor.
- **Saída**:
  - Fechar o comprado quando `SELL volume / BUY volume > 1 / DiffVolumes` ou `SELL orders / BUY orders > 1 / DiffTraders`
  - Fechar o vendido quando `SELL volume / BUY volume < DiffVolumes` ou `SELL orders / BUY orders < DiffTraders`
  - Fechar todas as posições fora do horário de trading
- **Stops**: Usa `Stop Loss` e `Take Profit` medidos em pontos de preço.

## Parâmetros
- `MinVolume` – volume total mínimo exigido em qualquer lado do livro (padrão: 20000)
- `MinTraders` – número mínimo de ordens em qualquer lado (padrão: 1000)
- `DiffVolumesEx` – proporção de volume exigida para entrada (padrão: 2.0)
- `DiffTradersEx` – proporção de contagem de ordens exigida para entrada (padrão: 1.5)
- `MinDiffVolumesEx` – proporção de volume usada após abertura de posição (padrão: 1.5)
- `MinDiffTradersEx` – proporção de contagem de ordens usada após abertura de posição (padrão: 1.3)
- `SleepMinutes` – atraso entre verificações do livro de ordens em minutos (padrão: 5)
- `TpPips` – take profit em pontos de preço (padrão: 500)
- `SlPips` – stop loss em pontos de preço (padrão: 500)

## Notas
A estratégia não inclui uma versão em Python.
