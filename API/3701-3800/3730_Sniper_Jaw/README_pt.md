# Estratégia de mandíbula de atirador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Sniper Jaw Strategy** transporta o MetaTrader 4 consultor especialista `SniperJawEA.mq4` para a estratégia de alto nível de StockSharp API. O sistema analisa o indicador Bill Williams' Alligator no preço médio da vela. Uma negociação só é iniciada quando as três médias móveis suavizadas (mandíbula, dentes e lábios) são empilhadas em ordem estrita de alta ou baixa e todas elas avançam na mesma direção em comparação com a vela finalizada anterior.

## Lógica de negociação

1. **Alligator reconstrução** – três instâncias `SmoothedMovingAverage` calculam a mandíbula, os dentes e os lábios na mediana da vela `(High + Low) / 2`. Cada linha pode ser deslocada para frente em seu próprio número de barras para espelhar a plotagem de MetaTrader.
2. **Confirmação de tendência** – uma tendência longa é produzida quando os valores deslocados satisfazem `jaw < teeth < lips` **e** cada linha é mais alta do que na vela anterior. Uma tendência curta precisa de `jaw > teeth > lips` com todas as três linhas se movendo para baixo em comparação com a barra anterior.
3. **Gerenciamento de entradas** – a estratégia abre apenas uma posição por vez. Quando `UseEntryToExit` está ativado e um novo sinal oposto é acionado, a exposição atual é nivelada primeiro e a nova ordem é enviada no próximo sinal.
4. **Saídas de proteção** – as distâncias de stop-loss e take-profit são definidas em pips e convertidas usando o título `PriceStep`. Ambas as posições longas e curtas são supervisionadas em cada vela finalizada e fechadas quando um dos limites é atingido.
5. **Aceleração de sinal** – o EA original evitou entradas duplicadas verificando o carimbo de data/hora da barra. A porta armazena o último horário da vela de sinal e pula ordens adicionais durante a mesma barra.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Tamanho da negociação em lotes ou contratos repassados para `BuyMarket`/`SellMarket`. |
| `EnableTrading` | `true` | Chave mestre que permite desabilitar novas entradas mantendo ativa a gestão de riscos. |
| `UseEntryToExit` | `true` | Fecha uma posição existente antes de armar um sinal oposto. Espelha o sinalizador "Entrada para Saída" do EA. |
| `StopLossPips` | `20` | Distância do stop de proteção ao preço de entrada. Zero desativa a parada. |
| `TakeProfitPips` | `50` | Distância da meta de lucro ao preço de entrada. Zero desativa o alvo. |
| `MinimumBars` | `60` | Número necessário de velas concluídas antes que o primeiro sinal seja avaliado. |
| `JawPeriod` / `TeethPeriod` / `LipsPeriod` | `13 / 8 / 5` | Comprimento das médias móveis suavizadas formando as linhas Alligator. |
| `JawShift` / `TeethShift` / `LipsShift` | `8 / 5 / 3` | Deslocamento direto (em barras) usado para alinhar os buffers Alligator com a versão MetaTrader. |
| `CandleType` | `1 hour time frame` | Assinatura da série de velas primárias. Ajuste para corresponder ao gráfico usado em MetaTrader. |

## Notas de uso

- A implementação avalia apenas velas finalizadas (`CandleStates.Finished`) para evitar valores parcialmente formados.
- Os níveis de parada e meta são rastreados internamente; a estratégia emite ordens de mercado para nivelar a posição quando um nível é violado.
- A conversão da etapa de preço segue a convenção comum do Forex: símbolos de 5 e 3 decimais tratam um pip como dez etapas de preço.
- Adicione a estratégia a um esquema juntamente com um conector, um portfólio e uma configuração de segurança. Após iniciar a estratégia, o painel do gráfico exibirá a série de velas e as linhas Alligator reconstruídas para rápida validação visual.
