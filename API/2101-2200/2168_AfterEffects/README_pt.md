# Estratégia AfterEffects
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia AfterEffects é baseada na ideia de que os preços de mercado podem mostrar efeitos residuais.
Ela calcula um sinal usando o preço de fechamento atual e as aberturas de `p` e `2p` barras atrás:

`signal = Close - 2 * Open[p] + Open[2p]`

Um sinal positivo abre uma posição comprada, enquanto um sinal negativo abre uma posição vendida.
Configurar `Random` como verdadeiro inverte o sinal.

Uma vez em posição, a estratégia coloca um stop-loss a `StopLoss` pontos da entrada.
Quando o preço se move `2 * StopLoss` pontos na direção favorável:

- se o sinal muda de sinal, a posição é revertida negociando com o dobro do volume;
- caso contrário, o stop-loss é ajustado para o novo nível.

## Detalhes

- **Critérios de entrada**: `signal > 0` para comprado, `signal < 0` para vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop-loss.
- **Stops**: Trailing.
- **Valores padrão**:
  - `StopLoss` = 500
  - `Period` = 3
  - `Random` = false
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Fórmula personalizada
  - Stops: Trailing
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
