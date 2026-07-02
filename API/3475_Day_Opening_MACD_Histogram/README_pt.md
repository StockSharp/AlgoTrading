# Estratégia de histograma de abertura do dia MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o especialista MetaTrader "2 1000 1 0,7% 0,5 500lev st" inserindo uma negociação no início de cada novo dia de negociação e filtrando a direção com a inclinação do histograma MACD. O sistema foi projetado para velas horárias e depende de parâmetros fixos de gerenciamento de dinheiro convertidos das configurações originais MQL.

## Lógica de negociação
- A estratégia monitora velas horárias e detecta a primeira vela de cada novo dia.
- Ele avalia o histograma MACD nas duas velas concluídas mais recentes do dia anterior.
- Se o histograma cair entre essas duas barras, o sistema abre uma posição longa na primeira vela do novo dia.
- Se o histograma aumentar, ele abre uma posição curta.
- Apenas uma posição pode estar ativa por vez. Os sinais opostos fecham a negociação atual antes de abrir a nova direção.

## Gestão de risco
- Distância inicial do stop loss: 875 pontos (convertido em preço multiplicando pela etapa de preço do instrumento).
- Distância de lucro: 510 pontos.
- Distância de parada final: 2.172 pontos. O stop segue o preço mais alto (longo) ou mais baixo (curto) alcançado desde a entrada e substitui o stop inicial quando este se torna mais apertado.
- A opção de ponto de equilíbrio original foi desativada e, portanto, omitida aqui.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Série de velas utilizadas pela estratégia (de hora em hora por padrão). | Velas de 1 hora |
| `MacdFastPeriod` | Período EMA rápido para o MACD. | 58 |
| `MacdSlowPeriod` | Período EMA lento para o MACD. | 195 |
| `MacdSignalPeriod` | Período da linha de sinal para o MACD. | 183 |
| `StopLossPoints` | Distância de stop-loss expressa em pontos do instrumento. | 875 |
| `TakeProfitPoints` | Distância de lucro em pontos. | 510 |
| `TrailingStopPoints` | Distância de parada final em pontos. | 2172 |

## Notas
- A estratégia usa apenas velas concluídas para evitar antecipação intrabarra, espelhando a opção "Usar valor da barra anterior" do especialista fonte.
- As saídas móveis e fixas são tratadas internamente, portanto, as proteções adicionais do portfólio devem permanecer desativadas para evitar o tratamento duplo de stops.
- A lógica pressupõe que a corretora usa definições de pontos padrão (etapa de preço). Ajuste os parâmetros se o instrumento usar um tamanho de tick diferente.
