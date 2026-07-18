# Estratégia Symr de Nova Barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Symr de Nova Barra** demonstra como detectar o início de novos candles em múltiplos períodos usando uma única assinatura. A estratégia monitora um período base e calcula quando intervalos maiores como 5m, 15m, 30m, 1h, 4h, 1d, 20m e 55m começam. Cada barra detectada é registrada no log.

## Detalhes

- **Critérios de entrada**: Nenhum. A estratégia não coloca operações.
- **Critérios de saída**: Nenhum.
- **Comprado/Vendido**: Não aplicável.
- **Stops**: Nenhum stop é utilizado.

### Parâmetros

| Nome | Padrão | Descrição |
|------|--------|-----------|
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Período base para detecção de novas barras. |

### Notas

- Armazena o último tempo de abertura para cada período predefinido.
- Quando o período base avança, períodos maiores são avaliados e registrados se rolarem.
- Útil como modelo para tratamento de eventos em múltiplos períodos.
