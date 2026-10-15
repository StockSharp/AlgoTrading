# Missão Impossível Power Two Estratégia Aberta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader “Mission Impossible Power Two Open”. Ele monitora a direção da vela concluída mais recentemente e abre uma nova cesta de negociações nessa direção. Quando o preço se move contra a cesta ativa, a estratégia adiciona novas entradas médias de acordo com uma grade fixa de pips. O volume de cada nova entrada cresce com a perda flutuante da cesta, imitando a regra de dimensionamento baseada em `power` do EA original. As metas de saída são recalculadas após cada preenchimento para que toda a cesta compartilhe um único nível de take-profit e stop-loss.

## Lógica de negociação

1. **Detecção de sinal** – Em cada vela finalizada, a estratégia compara o fechamento da vela anterior com a sua abertura.
   - Se a vela anterior fechou acima de sua abertura, o sinal longo está ativo.
   - Se fechou abaixo da abertura, o sinal curto está ativo.
   - Uma barra interna (fechada igual a aberta) não produz nova cesta.
2. **Abertura da primeira negociação** – Se nenhuma grade estiver ativa na direção sinalizada, a estratégia coloca uma ordem de mercado com o tamanho `BaseVolume`.
3. **Grade de média** – Quando existe uma cesta, a estratégia continua medindo a distância entre o último preço preenchido e o fechamento atual.
   - Para posições compradas, uma nova entrada é adicionada quando o preço cai pelo menos `GridStepPips * PriceStep` abaixo do último preenchimento.
   - Para posições vendidas, a estratégia espera até que o preço suba na mesma distância acima do último preenchimento.
   - A grade para de adicionar novas posições depois que `MaxTrades` preenchimentos forem alcançados na respectiva direção.
4. **Volume dinâmico** – Antes de enviar cada novo pedido a estratégia calcula a perda não realizada da cesta, multiplica por `Power * 0.0001` e soma o resultado a `BaseVolume`. O tamanho final é arredondado para a etapa do volume de troca, limitado entre os limites de segurança e limitado por `MaxVolume`.
5. **Gerenciamento de saídas** – Após cada preenchimento, a estratégia recalcula as metas compartilhadas para toda a cesta:
   - Com uma posição única, o take-profit está a `TakeProfitFirstPips` de distância da entrada e o stop-loss está a `StopLossPips` de distância na direção oposta.
   - Com duas ou mais posições, ambos os níveis estão ancorados no preço médio ponderado pelo volume da cesta, usando `TakeProfitNextPips` para a distância alvo e `StopLossPips` para proteção.
   - Quando o preço atinge o take-profit ou o stop-loss, todas as posições nessa direção são fechadas no mercado.
6. **Cestas independentes** – Grades longas e curtas são rastreadas de forma independente. A estratégia pode manter ambos ao mesmo tempo quando chegam sinais alternados.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.01` | Tamanho do pedido inicial para uma nova cesta antes de dimensionar. |
| `MaxVolume` | `decimal` | `2` | Limite rígido para uma única ordem de mercado após arredondamento. |
| `Power` | `decimal` | `13` | Multiplicador aplicado à perda flutuante no cálculo do volume aditivo para novas entradas. |
| `StopLossPips` | `int` | `400` | Distância nas etapas de preço usadas para o stop loss compartilhado. |
| `TakeProfitFirstPips` | `int` | `15` | Distância de lucro para a primeira entrada em uma cesta. |
| `TakeProfitNextPips` | `int` | `7` | Distância de take-profit para cestas médias (duas ou mais entradas). |
| `GridStepPips` | `int` | `21` | Movimento adverso mínimo (em etapas de preço) antes que outra entrada média seja permitida. |
| `MaxTrades` | `int` | `16` | Número máximo de negociações de grade por direção. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Velas usadas para geração de sinal e gerenciamento de cesta. |

## Notas

- Os volumes de pedidos são sempre alinhados ao `VolumeStep` do instrumento, restritos pelos `MinVolume` e `MaxVolume` do título sempre que esses limites estiverem disponíveis no conselho de negociação.
- As máquinas de estado longas e curtas são totalmente separadas, permitindo que a estratégia mantenha cestas cobertas quando a direção do mercado muda rapidamente.
- Os níveis de proteção são recalculados a cada preenchimento e arredondados para o `PriceStep` mais próximo, correspondendo à rotina frequente de modificação de lucro realizada na versão MetaTrader.
- Nenhum buffer de indicador é usado; todas as decisões são baseadas em dados brutos de velas e informações de portfólio, assim como na fonte EA.
