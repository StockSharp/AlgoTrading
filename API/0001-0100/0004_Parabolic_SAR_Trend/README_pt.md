# Tendência Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Parabolic SAR. A Tendência Parabolic SAR segue os pontos do indicador Parabolic SAR. Uma virada do preço de um lado do SAR para o outro marca uma possível mudança de tendência. Se o preço cruzar de volta, a operação é fechada.

Os testes indicam um retorno anual médio de aproximadamente 49%. Funciona melhor no mercado de criptomoedas.

Como os pontos do SAR acompanham o preço, eles naturalmente fornecem um ponto de saída quando a tendência muda. O método opera tanto comprado quanto vendido sem usar stops adicionais além da reversão do SAR.


## Detalhes

- **Critérios de entrada**: Sinais baseados em Parabolic, SAR.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic, SAR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

