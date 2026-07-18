# Estratégias cruzadas de preços médios móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Este pacote contém duas portas de estratégia C# dos MetaTrader 5 exemplos localizados em `MQL/50198`:

* **`MovingAveragePriceCrossStrategy`** – um sistema minimalista de média móvel versus cruzamento de preços que negocia uma única posição por vez.
* **`MovingAverageMartingaleStrategy`** – uma versão aprimorada que aplica o dimensionamento de posição no estilo martingale após perdas, preservando a mesma lógica de cruzamento de preço/média.

Ambas as implementações dependem do StockSharp API de alto nível, usam assinaturas de velas para avaliação de sinal e expõem parâmetros compatíveis com MetaTrader para distâncias de stop-loss e take-profit.

## Arquivos

| Arquivo | Descrição |
| --- | --- |
| `CS/MovingAveragePriceCrossStrategy.cs` | Cruzamento de preço base/MA usando volume fixo e ordens de proteção estáticas. |
| `CS/MovingAverageMartingaleStrategy.cs` | Variante Martingale que aumenta o volume e as distâncias de proteção após perder negociações. |

## Lógica de negociação

### MovingAveragePriceCrossEstratégia

1. Assina velas do período configurado e calcula uma média móvel simples (`SMA`).
2. Avalia sinais apenas em velas finalizadas para imitar o comportamento do especialista MT5.
3. Detecta cruzamentos entre SMA e o preço de fechamento da vela usando as duas últimas velas concluídas:
   * **Venda** quando a média móvel subir acima do fechamento da vela (preço ultrapassado abaixo da média).
   * **Compre** quando a média móvel cair abaixo do fechamento da vela (preço cruzado acima da média).
4. Coloca uma única ordem de mercado por sinal se nenhuma posição estiver aberta no momento.
5. Aplica proteção automática via `StartProtection` com distâncias de MetaTrader pontos convertidas em compensações de preço absoluto.

### MovingAverageMartingaleEstratégia

1. Compartilha a mesma assinatura de vela e geração de sinal SMA que a estratégia base.
2. Rastreia o PnL realizado após cada posição fechada e armazena o último resultado da negociação.
3. Quando um novo sinal de cruzamento aparece e nenhuma posição está aberta:
   * Se a última negociação foi **prejudicadora**, multiplica o próximo volume de negociação por `VolumeMultiplier` (limitado a `MaxVolume`) e aumenta as distâncias de stop-loss e take-profit em `TargetMultiplier`.
   * Se a última negociação foi **lucrativa**, redefine o volume de negociação e as distâncias de proteção para seus valores iniciais.
4. Aplica `StartProtection` com as compensações ajustadas dinamicamente imediatamente antes de enviar a ordem de mercado.
5. Continua a negociar apenas uma posição por vez, correspondendo à lógica original do Expert Advisor.

## Gestão de risco

* Os níveis de proteção são expressos em MetaTrader pontos e automaticamente traduzidos em compensações de preço absoluto usando o tamanho do pip detectado (`PriceStep` ajustado para símbolos FX decimais de 3/5).
* A estratégia martingale mantém os multiplicadores de stop-loss e take-profit limitados para evitar distâncias descontroladas.
* O volume de posição está alinhado com `VolumeStep`, `MinVolume` e `MaxVolume` opcional do instrumento para evitar pedidos inválidos.

## Parâmetros

### Entradas compartilhadas

| Parâmetro | Estratégia | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | ambos | `1 minute` | Tipo de dados Candle usado para cálculo de sinal. |
| `MaPeriod` | ambos | `50` | Comprimento da média móvel simples. |

### MovingAveragePriceCrossEstratégia

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | `1` | Volume do pedido alinhado à etapa do instrumento. |
| `TakeProfitPoints` | `150` | Distância de lucro em MetaTrader pontos (0 desativações). |
| `StopLossPoints` | `150` | Distância de stop-loss em MetaTrader pontos (0 desabilita). |

### MovingAverageMartingaleEstratégia

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `StartingVolume` | `1` | Volume base restaurado após negociações lucrativas. |
| `MaxVolume` | `5` | Volume máximo após aplicação de multiplicadores. |
| `TakeProfitPoints` | `100` | Distância inicial de lucro em MetaTrader pontos. |
| `StopLossPoints` | `300` | Distância inicial do stop-loss em MetaTrader pontos. |
| `VolumeMultiplier` | `2` | Fator aplicado ao próximo volume de pedido após uma perda. |
| `TargetMultiplier` | `2` | Fator aplicado às distâncias de stop-loss e take-profit após uma perda. |

## Notas de uso

* MetaTrader “pontos” correspondem a um `PriceStep` para a maioria dos instrumentos; as estratégias se multiplicam automaticamente por 10 para símbolos FX de 3 ou 5 decimais para corresponder ao comportamento do MT5.
* Ambas as estratégias requerem apenas um título e irão ignorar os sinais enquanto uma posição estiver aberta, reproduzindo a guarda `PositionsTotal()` dos especialistas originais.
* Ative a otimização nos parâmetros expostos dentro do designer StockSharp para replicar o ajuste de entrada MT5.
