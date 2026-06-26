# 3100 Fechar Todas as Posições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Converte o utilitário MQL5 **Close all positions** em uma estratégia de alto nível do StockSharp.
- Observa velas terminadas do período configurado e acumula o lucro flutuante de cada posição aberta no portfólio atribuído.
- Quando o lucro flutuante é igual ou superior ao limiar, ordens a mercado são enviadas para zerar todos os ativos gerenciados pela estratégia (incluindo estratégias filhas) até que o livro esteja completamente fechado.
- O sinalizador `_closeAllRequested` espelha a variável MQL `m_close_all` para que as ordens de saída continuem sendo emitidas até que não restem posições.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `ProfitThreshold` | `decimal` | `10` | Lucro flutuante (em moeda da conta) necessário antes de a estratégia zerar cada posição aberta. Espelha `InpProfit` do EA. |
| `CandleType` | `DataType` | Período `1m` | Série de velas que define os momentos de "nova barra". A verificação de lucro é executada apenas quando uma vela termina, emulando a lógica `PrevBars` original. |

## Lógica de trading
1. A estratégia assina velas de `CandleType` e processa apenas barras terminadas, assim como o EA avaliava o lucro apenas em uma nova barra.
2. A cada barra terminada o helper `CalculateTotalProfit` recupera `Portfolio.CurrentProfit` (PnL flutuante incluindo comissão e swap). Se o adaptador não puder fornecer este valor ele recorre à soma dos valores individuais de `PnL` de posição.
3. Se o lucro flutuante calculado estiver abaixo de `ProfitThreshold`, nada acontece.
4. Assim que o lucro atinge o limiar, `_closeAllRequested` é definido como `true` e `CloseAllPositions()` é executado imediatamente.
5. `CloseAllPositions()` coleta cada ativo que tem uma exposição no portfólio ou em estratégias aninhadas e envia ordens a mercado na direção oposta ao volume atual (comprado → venda, vendido → compra).
6. O sinalizador `_closeAllRequested` permanece definido até que `HasAnyOpenPosition()` detecte que o portfólio está zerado, correspondendo ao comportamento MQL onde `m_close_all` permanecia verdadeiro até que todos os tickets estivessem fechados.

## Notas adicionais
- Apenas a implementação em C# é fornecida; a pasta Python é intencionalmente deixada vazia conforme os requisitos da tarefa.
- A estratégia não cancela ordens pendentes porque o script original apenas fechava posições de mercado.
- Usar `SetOptimize` em `ProfitThreshold` para explorar alvos de lucro alternativos através do otimizador do Designer se necessário.

## Arquivos
- `CS/CloseAllPositionsStrategy.cs`
