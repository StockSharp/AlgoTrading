# Estratégia do Modelo de Integração de Volume McClellan A-D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói uma linha avanço-declínio ponderada multiplicando a amplitude de preço da barra pelo seu volume. Duas EMAs dessa linha ponderada formam um oscilador no estilo McClellan.

Uma posição comprada é aberta quando o oscilador cruza acima de um limiar definido pelo usuário após estar abaixo dele. A operação é fechada automaticamente após um número fixo de barras.

## Detalhes

- **Entrada**: o oscilador cruza acima de `Long Entry Threshold` vindo de baixo.
- **Saída**: posição fechada após `Exit After Bars` velas.
- **Comprado/Vendido**: somente comprado.
- **Indicadores**: duas EMAs.
- **Stops**: Nenhum.
- **Período**: Configurável.
