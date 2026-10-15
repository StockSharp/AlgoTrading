# Estratégia de ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia de Breakout é um sistema de breakout de Donchian canais convertido do MetaTrader consultor especialista original `BreakoutStrategy.mq5`. Em cada barra concluída, a estratégia monitora a máxima mais alta e a mínima mais baixa em uma janela de lookback configurável e entra nas negociações assim que o preço ultrapassa esses limites. As posições abertas são protegidas por um canal de rastreamento derivado de um segundo cálculo Donchian, refletindo a lógica de rastreamento usada no especialista de origem.

## Lógica de negociação

1. **Canal de entrada** – Os preços mais altos e mais baixos em `EntryPeriod` barras são atrasados em `EntryShift` barras para evitar o uso da barra atual no cálculo de rompimento.
2. **Detecção de rompimento** – Um rompimento longo é acionado quando a máxima da barra toca a banda superior deslocada mais uma etapa de preço. Um pequeno rompimento é acionado quando a mínima da barra toca a banda inferior deslocada menos uma etapa de preço.
3. **Canal de saída** – Os preços mais altos e mais baixos em `ExitPeriod` barras são atrasados em `ExitShift` barras. A linha intermediária opcional pode restringir o trailing stop selecionando o máximo (para posições compradas) ou mínimo (para posições vendidas) entre as faixas externa e intermediária, replicando a opção "usar linha intermediária" de EA.
4. **Gerenciamento de posição** – A estratégia fecha uma posição longa existente quando a barra baixa perfura o nível final e fecha uma posição curta quando a barra alta toca o nível final curto. Sinais opostos nivelam qualquer exposição existente antes de entrar na nova direção.
5. **Dimensionamento de risco** – O tamanho da posição é derivado de `RiskPerTrade`. A estratégia obtém o patrimônio do portfólio, converte a distância do stop em dinheiro utilizando os instrumentos `PriceStep` e `StepPrice` e solicita o maior volume permitido que mantenha a perda próxima ao percentual configurado. Os volumes estão alinhados com o instrumento `VolumeStep`, `VolumeMin` e `VolumeMax`.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Tipo de dados que descreve a série de velas utilizada pela estratégia. O padrão são velas de 1 hora. |
| `EntryPeriod` | Janela de lookback para o canal de breakout. |
| `EntryShift` | Número de barras concluídas usadas como deslocamento ao avaliar o canal. `1` reproduz o comportamento original EA. |
| `ExitPeriod` | Janela de lookback para o canal de saída final. |
| `ExitShift` | Deslocamento em barras aplicado ao canal final. |
| `UseMiddleLine` | Quando habilitada, a linha do meio Donchian participa do cálculo do trailing stop, correspondendo à opção MQL5. |
| `RiskPerTrade` | Fração do patrimônio do portfólio arriscado por negociação (por exemplo, `0.01` para 1%). |

## Notas

- Todos os comentários dentro da implementação do C# são escritos em inglês, conforme exigido pelas diretrizes do repositório.
- A estratégia usa StockSharp recursos API de alto nível: assinaturas de velas, Donchian canais (`Highest`/`Lowest` indicadores) e indicadores de mudança para evitar buffers manuais.
- Nenhum teste automatizado é fornecido para esta conversão; valide o comportamento em seu próprio ambiente antes de implantar na produção.
