# Ichimoku Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores Ichimoku Cloud e Stochastic Oscillator.
Entra comprado quando o preço está acima de Kumo (nuvem), Tenkan > Kijun, e o Stochastic está em sobrevenda (< 20). Entra vendido quando o preço está abaixo de Kumo, Tenkan < Kijun, e o Stochastic está em sobrecompra (> 80).

Os testes indicam um retorno anual médio de aproximadamente 118%. Funciona melhor no mercado de ações.

O Ichimoku define a tendência e os níveis de suporte enquanto o Stochastic determina o momento de entrada nas correções. As operações abrem quando o oscilador reseta dentro da direção predominante da nuvem.

Traders que preferem indicadores estruturados podem achá-lo prático. Stops de ATR cobrem reversões abruptas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Price > Cloud && StochK < 20`
  - Vendido: `Price < Cloud && StochK > 80`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Rompimento da nuvem na direção oposta
- **Stops**: Usa os limites da nuvem Ichimoku
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Ichimoku Cloud, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

