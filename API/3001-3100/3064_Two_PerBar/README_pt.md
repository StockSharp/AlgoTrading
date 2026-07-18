# Estratégia Two Per Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O expert original do MetaTrader "Two PerBar" abre uma posição comprada e uma vendida no início de cada nova barra, fecha toda a cesta na próxima barra e opcionalmente aplica um multiplicador de volume similar ao martingale. O port do StockSharp mantém o mesmo ritmo rastreando explicitamente ambas as pernas cobertas e reagindo uma vez por vela finalizada. Todas as ordens são criadas através da API de alto nível e respeitam os metadados do instrumento (passo de preço, passo de volume e restrições de lote mínimo/máximo).

## Ciclo de trading
1. **Detecção de nova vela** – a estratégia assina a série de velas configurada via `SubscribeCandles`. Quando a vela chega com `State == CandleStates.Finished`, uma nova barra começou e o ciclo é executado.
2. **Avaliar hits de take-profit** – cada perna armazenada carrega seu próprio preço de entrada e nível de take-profit. Se o máximo ou mínimo da vela completada tocar esse nível, a perna é fechada imediatamente com uma ordem de mercado e removida da lista de rastreamento.
3. **Liquidação forçada de sobras** – quaisquer pernas que sobreviveram ao scan de take-profit são liquidadas ao mercado antes de abrir o próximo par. Isso espelha o código do MetaTrader que chama `PositionClose` em cada abertura de barra.
4. **Determinar o próximo tamanho de lote** –
   - Quando um ciclo anterior ainda tinha pernas abertas, o maior volume entre elas é multiplicado por `VolumeMultiplier`.
   - Quando a cesta terminou plana (por exemplo, ambas as pernas atingiram seu take-profit), o ciclo redefine para `InitialVolume`.
   - `PrepareVolume` normaliza o lote candidato arredondando para dois decimais, ajustando-o ao `VolumeStep` do instrumento, verificando contra o `MinVolume` da bolsa, e finalmente redefinindo para `InitialVolume` se exceder o `MaxVolume` definido pelo usuário ou o `Security.MaxVolume`.
5. **Atualizar valores padrão** – o lote calculado é armazenado dentro de `_lastCycleVolume` e escrito em `Strategy.Volume` para que os métodos auxiliares reutilizem a mesma quantidade.
6. **Criar um novo par coberto** – `BuyMarket(volume)` abre a perna comprada e `SellMarket(volume)` abre a perna vendida. Cada perna lembra o preço de fechamento da vela finalizada e o nível absoluto de take-profit (`entry ± TakeProfitPoints * pointSize`). Um `TakeProfitPoints` zero ou negativo desabilita o take-profit e apenas o passo de liquidação forçada encerrará a cesta.

O resultado é um straddle perpétuo: cada vela começa com um par comprado + vendido, ambos são inspecionados para alvos de lucro durante a barra, e tudo fica plano antes do próximo ciclo.

## Gestão de dinheiro e proteção
- **Escalonamento similar ao martingale** – `VolumeMultiplier` replica o multiplicador do MetaTrader. Quando qualquer perna sobrevive até o passo de liquidação forçada, o próximo ciclo usa o tamanho da perna mais pesada multiplicado por este valor. Um ciclo lucrativo completado (ambas as pernas fechadas via take-profit) redefine o lote para `InitialVolume`.
- **Teto de volume** – `MaxVolume` é um teto rígido que força o lote de volta para `InitialVolume` assim que o multiplicador o excederia. O mesmo reset acontece se o instrumento reportar um `Security.MaxVolume` mais restritivo.
- **Conformidade com a bolsa** – todos os volumes são ajustados ao `VolumeStep` do valor e rejeitados quando ficam abaixo de `MinVolume`. Definir `InitialVolume` para um tamanho negociável garante que o caminho de reset sempre permaneça válido.
- **Cálculo de pontos** – o offset de take-profit usa `Security.PriceStep` (ou `MinPriceStep` como fallback). Instrumentos sem um passo definido efetivamente desabilitam o take-profit porque o offset calculado é zero.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Período principal que aciona o fluxo de trabalho uma vez por barra. |
| `InitialVolume` | `decimal` | `1` | Tamanho do lote usado ao iniciar um novo ciclo sem pernas sobreviventes. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplicador aplicado à maior perna sobrevivente do ciclo anterior. |
| `MaxVolume` | `decimal` | `10` | Tamanho máximo de lote permitido antes de redefinir para `InitialVolume`. |
| `TakeProfitPoints` | `int` | `50` | Distância em pontos de preço usada para construir o alvo de take-profit por perna. `0` desabilita o take-profit e se baseia apenas na liquidação no fechamento da barra. |

## Notas de implementação e diferenças
- As pernas cobertas são rastreadas manualmente dentro de `_legs` para que a estratégia possa raciocinar sobre exposições longas/curtas individuais, embora o StockSharp reporte apenas a posição líquida.
- Em vez de depender de ticks individuais, a lógica de take-profit verifica o intervalo alto/baixo da vela completada. Isso mantém a implementação determinista enquanto permanece fiel ao comportamento original "por barra".
- As configurações de slippage e número mágico do MetaTrader não estão expostas; o StockSharp gerencia os detalhes de roteamento de ordens, e a estratégia é executada no portfólio associado à instância de estratégia pai.
- A colocação de ordens usa os métodos auxiliares de `Strategy` (`BuyMarket`, `SellMarket`) sem adicionar indicadores diretamente a `Strategy.Indicators`, cumprindo as diretrizes do repositório.

## Dicas de uso
- Ajuste `InitialVolume` ao passo de lote do instrumento antes de iniciar a estratégia. O construtor não tenta arredondar automaticamente sua entrada.
- Se o instrumento tiver um passo de preço muito pequeno, considere reduzir `TakeProfitPoints`; caso contrário, o take-profit calculado pode ficar irrealisticamente distante.
- Como a estratégia abre ordens em direções opostas ao mesmo tempo, execute-a em conectores/bolsas que permitam posições cobertas. Em ambientes que neteiam posições imediatamente, a lista `_legs` ainda reflete a lógica pretendida, mas o comportamento real do broker pode diferir.
- Adicione a estratégia a um gráfico para visualizar velas e operações executadas (`DrawCandles` + `DrawOwnTrades` estão habilitados em `OnStarted`).
