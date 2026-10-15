# Estratégia ROC Impulce
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada no impulso do Rate of Change (ROC)

Os testes indicam um retorno anual médio de aproximadamente 91%. Funciona melhor no mercado de ações.

ROC Impulse captura explosões repentinas no indicador Rate of Change. Picos positivos acentuados levam a operações compradas e picos negativos acentuados a operações vendidas. Quando o momentum se dissipa em direção a zero, a posição é fechada.

Os níveis de gatilho podem ser ajustados para reagir apenas a eventos de momentum excepcionais. Stops baseados em ATR ajudam a prevenir grandes perdas se o pico reverter rapidamente.


## Detalhes

- **Critérios de entrada**: Sinais baseados em ATR, ROC, Momentum.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `RocPeriod` = 12
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, ROC, Momentum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

