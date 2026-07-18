# Estratégia de Filtro Elíptico Ótimo Modificado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica o indicador *Modified Optimum Elliptic Filter* descrito por John F. Ehlers para detectar mudanças de direção. O indicador é um filtro digital de dois polos que suaviza a média dos preços máximos e mínimos usando a seguinte fórmula recursiva:

```
F(t) = 0.13785*(2*HL2(t) - HL2(t-1))
     + 0.0007 *(2*HL2(t-1) - HL2(t-2))
     + 0.13785*(2*HL2(t-2) - HL2(t-3))
     + 1.2103 * F(t-1) - 0.4867 * F(t-2)
```

Onde `HL2` é o ponto médio `(High + Low)/2` de cada candle.

A estratégia lê os últimos três valores do filtro para determinar o momentum. Se o indicador está subindo e o valor mais recente supera o anterior, uma posição comprada é aberta. Se o indicador está caindo e o valor atual está abaixo do anterior, uma posição vendida é aberta. As posições são revertidas quando a condição oposta ocorre.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `F(t-1) < F(t-2)` e `F(t) > F(t-1)`.
  - **Vendido**: `F(t-1) > F(t-2)` e `F(t) < F(t-1)`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - A posição é revertida no sinal oposto.
- **Stops**: Sem stops explícitos.
- **Valores padrão**:
  - `Candle Type` = período de 4 horas.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
