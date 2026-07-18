# Estratégia de Cobertura de Sobreposição Multicurrency
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do consultor especializado MetaTrader 4 **"Multicurrency hedge example EA (overlay hedge)"** para a API de alto nível do StockSharp.

## Visão geral
- Trabalha com um universo de símbolos forex fornecidos pelo usuário e monitora todos os pares únicos.
- Calcula correlação de Pearson deslizante e ratios de ATR para determinar quais símbolos se movem juntos e como dimensionar ambas as pernas.
- Constrói sobreposições de preço sintéticas para detectar quando o instrumento principal se desvia de seu parceiro correlacionado além de um limiar configurável.
- Abre blocos cobertos (compra/venda, compra/compra, venda/compra, venda/venda) dependendo do sinal de correlação e da direção de sobreposição.
- Fecha o bloco inteiro assim que um alvo de take-profit mútuo é atingido em pontos ou na moeda do portfólio.

## Fluxo de trabalho
1. Inscrever-se em velas completadas para cada instrumento no universo e armazenar os últimos valores de high/low/close.
2. Inscrever-se em cotações Level1 de cada instrumento para aplicar filtros de spread antes de enviar coberturas.
3. Uma vez por dia (padrão 01:00 no horário do servidor) reconstruir a lista de pares negociáveis:
   - Manter apenas pares onde a correlação absoluta está acima do limiar configurado.
   - Calcular o ratio de ATR para escalar o volume da perna principal.
4. Para cada vela completada verificar a distância de sobreposição:
   - Correlação positiva ⇒ comprar principal / vender secundária quando o desvio está abaixo de `-OverlayThreshold` pontos, vender principal / comprar secundária quando está acima de `+OverlayThreshold` pontos.
   - Correlação negativa ⇒ comprar ambas as pernas abaixo do limiar negativo, vender ambas as pernas acima do limiar positivo.
5. Rastrear blocos de cobertura abertos e fechá-los quando o lucro agregado atingir qualquer uma das condições de take-profit.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Universe` | Coleção de objetos `Security` para escanear. Precisa de pelo menos duas entradas. | vazio |
| `CandleType` | Tipo de dados de velas usado para cálculos. | Período de 1 minuto |
| `RangeLength` | Número de barras usadas para calcular envelopes de preço. | 400 |
| `CorrelationLookback` | Barras usadas para correlação de Pearson. | 500 |
| `AtrLookback` | Barras usadas para dimensionamento do ratio de ATR. | 200 |
| `CorrelationThreshold` | Correlação absoluta mínima para manter um par (0–1). | 0.90 |
| `OverlayThreshold` | Distância de sobreposição em pontos medida usando o passo do instrumento principal. | 100 |
| `TakeProfitByPoints` / `TakeProfitPoints` | Habilita e configura take-profit mútuo baseado em pontos. | true / 10 |
| `TakeProfitByCurrency` / `TakeProfitCurrency` | Habilita e configura take-profit mútuo baseado em moeda. | false / 10 |
| `MaxOpenPairs` | Máximo de blocos de cobertura abertos simultaneamente. | 10 |
| `BaseVolume` | Volume da perna secundária (volume da perna principal = `BaseVolume * ATR ratio`). | 1 |
| `RecalculationHour` | Hora do dia em que as correlações são recalculadas. | 1 |
| `MaxSpread` | Spread bid-ask máximo permitido por perna (em pontos). | 10 |

## Requisitos de dados
- Velas históricas e ao vivo para cada instrumento em `Universe` com o `CandleType` especificado.
- Atualizações de cotações Level1 para cada instrumento para validar spreads.
- Informações de portfólio para registro de ordens.

## Notas de uso
- A estratégia não auto-popula o universo; passe os símbolos forex desejados antes de iniciar.
- Para imitar a lógica de dimensionamento do MetaTrader, mantenha `BaseVolume` igual ao tamanho de lote da perna secundária. O volume da perna principal é automaticamente escalado pelo ratio de ATR.
- Se os dados de spread não estiverem disponíveis, a estratégia pulará novas entradas até que o primeiro snapshot do livro de ordens chegue.
- A lógica de fechamento estima o lucro mútuo combinando o movimento com sinal de cada perna usando o passo de preço do instrumento e o preço do passo.

## Diferenças do EA original
- Usa assinaturas StockSharp (`SubscribeCandles`, `SubscribeLevel1`) em vez de polling baseado em temporizador.
- A lógica de take-profit é implementada com informações médias do passo de preço em vez de lucro/comissão bruta de operação.
- Requer um parâmetro de universo explícito, permitindo que a estratégia seja executada em qualquer subconjunto de instrumentos suportados pelo StockSharp.
- A execução de ordens é realizada através de ordens a mercado do StockSharp com comentários por cobertura para rastreabilidade.
