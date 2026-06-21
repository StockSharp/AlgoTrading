# Estratégia de Limiar UltraFATL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador UltraFATL para detectar mudanças na força da tendência. O indicador gera níveis discretos de 0 a 8. Uma posição comprada é aberta quando o valor anterior está acima do nível 4 e o valor atual cai abaixo de 5, mantendo-se positivo. Uma posição vendida é aberta quando o valor anterior está abaixo de 5 mas acima de zero e o valor atual sobe acima de 4. O algoritmo trabalha com velas de 4 horas por padrão, mas o período pode ser ajustado.

A abordagem espera continuação da tendência após um recuo a partir de leituras extremas de UltraFATL. As posições são revertidas quando a condição oposta aparece.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `UltraFATL(prev) > 4` e `UltraFATL(curr) < 5` e `UltraFATL(curr) != 0`.
  - **Vendido**: `UltraFATL(prev) < 5` e `UltraFATL(prev) != 0` e `UltraFATL(curr) > 4`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O sinal oposto reverte a posição.
- **Stops**: Não utilizados por padrão.
- **Valores padrão**:
  - `Candle Type` = velas de 4 horas.
  - `Length` = 3.
  - `Signal Bar` = 1 (usar a barra anterior para sinais).
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único (UltraFATL)
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
