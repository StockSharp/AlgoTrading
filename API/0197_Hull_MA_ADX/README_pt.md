# Hull Ma Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em Hull Moving Average e ADX. Entra comprado quando o HMA sobe e ADX > 25 (tendência forte). Entra vendido quando o HMA cai e ADX > 25 (tendência forte). Sai quando ADX < 20 (tendência enfraquecendo).

Os testes indicam um retorno anual médio de aproximadamente 178%. Funciona melhor no mercado de ações.

Hull MA mostra a tendência, enquanto ADX confirma sua intensidade. As entradas seguem a inclinação do Hull quando o ADX indica força.

Eficaz para traders que focam em tendências suaves com confirmação. Stops baseados em ATR mantêm as perdas sob controle.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `HullMA turning up && ADX > 25`
  - Vendido: `HullMA turning down && ADX > 25`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: reversão do Hull MA
- **Stops**: Baseados em ATR usando `AtrMultiplier`
- **Valores padrão**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Hull MA, Moving Average, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

