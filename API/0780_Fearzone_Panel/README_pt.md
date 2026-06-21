# Painel Fearzone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia inspirada no painel FearZone de «Framgångsrik Aktiehandel». Procura vendas de pânico onde o medo domina.

A estratégia aguarda que ambos os indicadores Fearzone estejam ativos e pelo menos um gatilho de pânico seja acionado, enquanto o preço permanece acima da média móvel de 200 períodos.

## Detalhes

- **Critérios de entrada**: FZ1 e FZ2 ativos mais impulso negativo, zona de ricochete ou estocástico em sobrevenda, com fechamento acima de MA200.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: O preço cai abaixo de MA200.
- **Stops**: Não.
- **Valores padrão**:
  - `LookbackPeriod` = 22
  - `BollingerPeriod` = 200
  - `ImpulsePeriod` = 10
  - `ImpulsePercent` = 0.1m
  - `MaPeriod` = 200
  - `StochThreshold` = 30m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: BollingerBands, RateOfChange, StochasticOscillator, SimpleMovingAverage, Highest
  - Stops: Não
  - Complexidade: Moderado
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
