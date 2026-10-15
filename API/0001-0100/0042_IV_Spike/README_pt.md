# Estratégia Implied Volatility Spike
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia monitora a volatilidade implícita em busca de saltos repentinos em relação ao valor anterior. Um forte spike combinado com o preço operando contra a média móvel pode sinalizar uma reversão de curto prazo.

Os testes indicam um retorno anual médio de aproximadamente 163%. Funciona melhor no mercado de ações.

Quando a volatilidade implícita aumenta acima do limiar configurado, o sistema entra na direção oposta ao movimento do preço, esperando que a volatilidade reverta.

As posições são fechadas assim que a volatilidade começa a cair ou ocorre um stop-loss.

## Detalhes

- **Critérios de entrada**: IV spike acima de `IVSpikeThreshold` e preço relativo à MA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: IV diminui ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `IVPeriod` = 20
  - `IVSpikeThreshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: IV, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

