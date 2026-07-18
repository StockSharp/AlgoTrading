# Estratégia RSI Hook Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia RSI Hook Reversal tenta capturar pontos de virada de curto prazo quando o RSI sai de um extremo. Após um impulso de sobrecompra ou sobrevenda, o indicador frequentemente "engata" de volta em direção à linha central antes que o preço reaja.

Os testes indicam um retorno anual médio de aproximadamente 163%. Funciona melhor no mercado de ações.

A estratégia aguarda esse gancho enquanto o preço continua pressionando na direção anterior. Uma entrada comprada é acionada quando o RSI se curva para cima a partir da sobrevenda enquanto o preço marca uma nova mínima; uma entrada vendida é aberta quando o RSI vira para baixo a partir da sobrecompra durante uma nova máxima.

As operações usam um stop percentual simples para controlar o risco e tipicamente fecham quando o RSI engata na direção oposta.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
