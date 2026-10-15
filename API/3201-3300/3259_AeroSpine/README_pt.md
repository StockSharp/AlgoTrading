# Estratégia de AeroSpine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de AeroSpine é uma conversão do especialista MetaTrader **AEROSPINE.mq4**. Ela opera um único símbolo e tenta capturar rompimentos afastados do preço de abertura diária. O robô original foi projetado para gráficos diários enquanto monitorava ticks; o port mantém a ideia de rompimento da abertura diária, mas depende de velas concluídas fornecidas pelo StockSharp.

## Lógica de trading
- No início de cada dia de trading, a estratégia armazena o preço de abertura diária derivado da primeira vela do dia.
- Novas posições são avaliadas apenas após a hora de entrada configurada. As velas concluídas devem satisfazer um filtro de volume mínimo e o spread atual deve estar abaixo do limite configurado.
- Se nenhuma posição estiver aberta e nenhum trade de recuperação estiver pendente:
  - Um trade **comprado** é aberto quando a máxima da vela cruza a abertura diária em `EntryOffsetPips`.
  - Um trade **vendido** é aberto quando a mínima da vela rompe abaixo da abertura diária em `EntryOffsetPips`.
- Após qualquer trade perdedor, a estratégia prepara uma entrada de recuperação na direção oposta. Trades de recuperação usam `RecoveryOffsetPips` e aumentam o volume adicionando o volume base ao tamanho do trade perdedor, replicando o dimensionamento estilo martingale do especialista MQL.
- Posições abertas são gerenciadas com três mecanismos:
  - Um take-profit fixo em `TakeProfitPips` a partir do preço de entrada.
  - Um gatilho opcional de break-even que fecha o trade quando o preço recua para a distância de break-even após ter se movido a favor da posição.
  - Uma saída protetora se o preço retornar à abertura diária e cruzá-la em `ExitOffsetPips` contra a posição.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| **Candle Type** | Período das velas de trabalho usadas para avaliação de sinais. |
| **Volume** | Tamanho base da ordem para primeiras entradas e para construir o volume de recuperação. |
| **Entry Hour** | Hora mínima (horário da bolsa) em que novas entradas podem ser realizadas. |
| **Entry Offset** | Distância em pips da abertura diária que deve ser cruzada para abrir o primeiro trade do dia. |
| **Exit Offset** | Distância em pips além da abertura diária usada para fechar posições que revertam além da abertura. |
| **Recovery Offset** | Distância em pips da abertura diária necessária para acionar um trade de recuperação após uma perda. |
| **Take Profit** | Distância fixa de take-profit medida em pips a partir do preço de entrada. |
| **Break Even** | Distância em pips necessária para armar a saída de break-even. |
| **Use Break Even** | Ativa ou desativa o bloco de gerenciamento de break-even. |
| **Volume Filter** | Volume mínimo da vela necessário para novas entradas, espelhando a verificação original `Volume[0] > 10000`. |
| **Max Spread** | Rejeita novas entradas se o spread atual for mais amplo que o valor permitido (convertido de pips). |
| **Enable Recovery** | Ativa a lógica de recuperação na direção oposta após um trade perdedor. |

## Notas sobre a conversão
- O EA original colocava ordens diretamente em ticks enquanto aplicava um gráfico diário. O port emula isso com velas intradíarias: a abertura diária é atualizada na primeira vela de cada dia e as verificações de rompimento usam as máximas/mínimas das velas.
- Todos os elementos de interface do MetaTrader (rótulos, cálculos de capital em múltiplos símbolos, etc.) foram removidos. Apenas a lógica de trading relevante para o símbolo atual foi preservada.
- Break-even e modificações de stop (`OrderModify`) são simulados via chamadas explícitas a `ClosePosition()` quando os limites calculados são atingidos.
- Filtros de spread e volume mapeiam diretamente às verificações originais `MODE_SPREAD` e `Volume[0]`.
