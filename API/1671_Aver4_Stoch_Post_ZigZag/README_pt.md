# Aver4 Stoch Post ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina quatro osciladores Stoch em múltiplos horizontes de tempo e um detector simples de pivôs ZigZag. O Stoch médio orienta os níveis de sobrecompra/sobrevenda enquanto o ZigZag confirma máximas e mínimas de oscilação. Compras ocorrem quando o Stoch médio cai abaixo do nível de sobrevenda e uma nova mínima ZigZag se forma. Vendas ocorrem quando o Stoch médio sobe acima do nível de sobrecompra e uma nova máxima ZigZag se forma. Posições opostas existentes são fechadas na reversão do sinal.

## Detalhes
- **Critérios de entrada**: Stoch médio cruzando zonas de sobrevenda/sobrecompra com pivô ZigZag correspondente.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: StartProtection 2%/2% (padrão).
- **Valores padrão**:
  - `ShortLength` = 26
  - `MidLength1` = 72
  - `MidLength2` = 144
  - `LongLength` = 288
  - `ZigZagDepth` = 14
  - `Oversold` = 5
  - `Overbought` = 95
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic, ZigZag
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
